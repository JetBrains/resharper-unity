using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using IGotoStatement = JetBrains.ReSharper.Psi.CSharp.Tree.IGotoStatement;
using IIfStatement = JetBrains.ReSharper.Psi.CSharp.Tree.IIfStatement;
using ILoopStatement = JetBrains.ReSharper.Psi.CSharp.Tree.ILoopStatement;
using IReturnStatement = JetBrains.ReSharper.Psi.CSharp.Tree.IReturnStatement;
using QualifierEqualityComparer = JetBrains.ReSharper.Psi.CSharp.Impl.ControlFlow.ControlFlowWeakVariableInfo.QualifierEqualityComparer;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(
        ElementTypes: new [] { typeof(IParametersOwnerDeclaration), typeof(IPropertyDeclaration)}, 
        HighlightingTypes = new[] {typeof(InefficientPropertyAccessWarning)})]
    public class ComponentPropertyAccessProblemAnalyzer : UnityElementProblemAnalyzer<ITreeNode>
    {
        private static readonly QualifierEqualityComparer ourComparer = new QualifierEqualityComparer();
        
        // NB : this analyzer invalidates all cached references (create a separated group of references which can be cached and
        // pass them to quick fix) when encounter branches in control flow graph.
        // e.g : invalidate before if, after else branch, after then branch, before loop, after loop section and so on.
        
        public ComponentPropertyAccessProblemAnalyzer([NotNull] UnityApi unityApi)
            : base(unityApi)
        {
        }
        
        protected override void Analyze(ITreeNode node, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {
            // container to collect groups of IReferenceExpressions which can be cached
            var container = new PropertiesAccessContainer(consumer);

            ICSharpTreeNode scope = null;
            switch (node)
            {
                case ICSharpParametersOwnerDeclaration parametersOwnerDeclaration:
                    var codeBody = parametersOwnerDeclaration.GetCodeBody();
                    scope = codeBody.BlockBody ?? (ICSharpTreeNode)codeBody.ExpressionBody;
                    break;
                case ILambdaExpression lambdaExpression:
                    codeBody = lambdaExpression.GetCodeBody();
                    scope = codeBody.BlockBody ?? (ICSharpTreeNode)codeBody.ExpressionBody;
                    break;
                case IPropertyDeclaration propertyDeclaration:
                    var initial = propertyDeclaration.Initial;
                    if (initial != null)
                    {
                        scope = initial;
                    }
                    else
                    {
                        foreach (var accessorDeclaration in propertyDeclaration.AccessorDeclarationsEnumerable)
                        {
                            codeBody = accessorDeclaration.GetCodeBody();
                            var body = codeBody.BlockBody ?? (ICSharpTreeNode)codeBody.ExpressionBody;
                            if (body != null)
                            {
                                AnalyzeStatements(body, container);
                                container.InvalidateCachedValues();
                            }
                        }
                    }
                    break;
                default:
                    return;
            }
            
            if (scope == null)
                return;
            
            AnalyzeStatements(scope, container);
            
            // cache all references which is not invalidated dut to no related expression was found
            container.InvalidateCachedValues();
        }

        private void AnalyzeStatements([NotNull] ICSharpTreeNode scope, [NotNull] PropertiesAccessContainer referencesContainer)
        {
            var visitor = new PropertiesAnalyzerVisitor(referencesContainer, scope, 
                new Dictionary<IReferenceExpression, IEnumerator<ITreeNode>>(ourComparer),
                new HashSet<IReferenceExpression>(ourComparer));
            scope.ProcessThisAndDescendants(visitor);
        }

        private static bool IsUnityComponentProperty(IReferenceExpression referenceExpression)
        {
            var info = referenceExpression.Reference.Resolve();
            if (info.ResolveErrorType != ResolveErrorType.OK)
                return false;
    
            var property = info.DeclaredElement as IProperty;
            var containingType = property?.GetContainingType();
            if (containingType == null) 
                return false;

            return containingType.GetAllSuperTypes().Any(t => t.GetClrName().Equals(KnownTypes.Component));
        }
        
        private static bool IsReferenceExpressionOnly(IReferenceExpression referenceExpression)
        {
            var qualifier = referenceExpression.QualifierExpression.GetContainingParenthesizedExpression();
            while (qualifier != null)
            {
                if (qualifier is IReferenceExpression next)
                {
                    qualifier = next.QualifierExpression.GetContainingParenthesizedExpression();
                }
                else if (qualifier is IThisExpression)
                {
                    return true;
                } 
                else
                {
                    return false;
                }
            }

            return true;
        }

        private static UnityComponentRelatedReferenceExpressionFinder GetFinder(IReferenceExpression referenceExpression)
        {
            // Register here custom finders for unity components. 
            var declaredElement = (referenceExpression.Reference.Resolve().DeclaredElement as IClrDeclaredElement).NotNull("declaredElement != null");
            var containingType = declaredElement.GetContainingType().NotNull("declaredElement.GetContainingType() != null");

            if (containingType.GetClrName().Equals(KnownTypes.Transform))
                return new TransformRelatedReferenceFinder(referenceExpression);
            
            return new UnityComponentRelatedReferenceExpressionFinder(referenceExpression);
        }
        
        private class PropertiesAccessContainer
        {
            private readonly IHighlightingConsumer myConsumer;
            
            // Groups of references that can be cached. Each group can be invalidated independently
            private readonly IDictionary<IReferenceExpression, List<IReferenceExpression>> myPropertiesMap = new Dictionary<IReferenceExpression, List<IReferenceExpression>>(ourComparer);

            public PropertiesAccessContainer(IHighlightingConsumer consumer)
            {
                myConsumer = consumer;
            }

            public void AddProperty(IReferenceExpression referenceExpression)
            {
                if (!myPropertiesMap.ContainsKey(referenceExpression)) 
                    myPropertiesMap[referenceExpression] = new List<IReferenceExpression>();
                myPropertiesMap[referenceExpression].Add(referenceExpression);
            }

            public void InvalidateCachedValues(IReferenceExpression key)
            {
                if (!myPropertiesMap.ContainsKey(key)) 
                    return;

                var highlighitingElements = myPropertiesMap[key];
                Assertion.Assert(highlighitingElements.Count > 0, "highlighitingElements.Length > 0");
                
                // calculate read/write operations for property
                int write = 0;
                int read = 0;
                int startHighlightIndex = -1;

                ICSharpTreeNode readAnchor = null;
                bool inlineCacheValue = false;
                ICSharpTreeNode writeAnchor = null;
                IReferenceExpression lastWriteExpression = null;
                bool inlineRestoreValue = false;
                
                for (int i = 0; i < highlighitingElements.Count; i++)
                {
                    var referenceExpression = highlighitingElements[i];

                    var accessType = referenceExpression.GetAccessType();
                    
                    if (read == 0 && write == 0)
                    {
                        readAnchor = referenceExpression.GetContainingStatementLike().NotNull("readAnchor != null");
                        var previousRelatedExpression = GetPreviousRelatedExpression(referenceExpression, readAnchor);
                        // if we have related expression before considered reference expression, we can only inline reading into statement
                        // Example:
                        // transform.Position = (transform.localPosition = Vector3.Up) + transform.position + transform.position;
                        //                       ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                        // transform.localPosition is related to transform.position, but we need cache our variable
                        // The result is:
                        // var cache = transform.position;
                        // transform.position = (transform.localPosition = Vector3.Up) + (cache = transform.position) + cache;

                        inlineCacheValue = previousRelatedExpression != null;
                    }
                    
                    if (accessType.HasFlag(ExpressionAccessType.Write))
                    {
                        write++;
                        lastWriteExpression = referenceExpression;
                    }
                    if (accessType.HasFlag(ExpressionAccessType.Read))
                    {
                        read++;
                    }

                    if (startHighlightIndex == -1 && (read == 2|| write == 2 | read + write == 3))
                    {
                        startHighlightIndex = i;
                    }
                }

                if (lastWriteExpression != null)
                {
                    writeAnchor = lastWriteExpression.GetContainingStatementLike().NotNull("writeAnchor != null");
                    if (writeAnchor is IReturnStatement ||
                        writeAnchor is IYieldStatement yieldStatement && yieldStatement.StatementType == YieldStatementType.YIELD_RETURN)
                    {
                        inlineRestoreValue = true;
                    } else {
                        var relatedExpressions = GetFinder(lastWriteExpression).GetRelatedExpressions(writeAnchor, lastWriteExpression);
                        inlineRestoreValue = relatedExpressions.Any();
                    }
                }

                if (startHighlightIndex != -1)
                {
                    for (int i = startHighlightIndex; i < highlighitingElements.Count; i++)
                    {
                        var warning = new InefficientPropertyAccessWarning(highlighitingElements[i], highlighitingElements, 
                            readAnchor, inlineCacheValue, writeAnchor, inlineRestoreValue);
                        myConsumer.AddHighlighting(warning);
                    }
                }

                myPropertiesMap.Remove(key);
            }

            private ITreeNode GetPreviousRelatedExpression(IReferenceExpression referenceExpression, ICSharpTreeNode readAnchor)
            {
                var finder = GetFinder(referenceExpression);
                var allSequence = finder.GetRelatedExpressions(readAnchor);
                var fromSequence = finder.GetRelatedExpressions(readAnchor, referenceExpression);
                var stopElement = fromSequence.FirstOrDefault();

                ITreeNode prev = null;
                foreach (var expression in allSequence)
                {
                    if (expression == stopElement)
                        break;
                    prev = expression;
                }

                return prev;
            }

            public void InvalidateCachedValues()
            {
                var keys = myPropertiesMap.Keys.ToArray();
                foreach (var key in keys)
                {
                    InvalidateCachedValues(key);
                }
            }
        }

        private class PropertiesAnalyzerVisitor : TreeNodeVisitor, IRecursiveElementProcessor
        {
            private readonly PropertiesAccessContainer myContainer;
            private readonly ICSharpTreeNode myScope;
            private readonly Dictionary<IReferenceExpression, IEnumerator<ITreeNode>> myReferenceInvalidateBarriers;
            private readonly HashSet<IReferenceExpression> myWithoutRelatedExpression;

            public PropertiesAnalyzerVisitor(PropertiesAccessContainer container, ICSharpTreeNode scope,
                Dictionary<IReferenceExpression, IEnumerator<ITreeNode>> referenceInvalidateBarriers,
                HashSet<IReferenceExpression> withoutRelatedExpression)
            {
                myContainer = container;
                myScope = scope;
                myReferenceInvalidateBarriers = referenceInvalidateBarriers;
                myWithoutRelatedExpression = withoutRelatedExpression;
            }

            public override void VisitReferenceExpression([NotNull] IReferenceExpression referenceExpression)
            {
                if (!IsUnityComponentProperty(referenceExpression))
                    return;

                if (!IsReferenceExpressionOnly(referenceExpression))
                    return;
                
                // first encounter of reference. Create group and find related expressions.
                if (!myReferenceInvalidateBarriers.ContainsKey(referenceExpression) && !myWithoutRelatedExpression.Contains(referenceExpression))
                {
                    var relatedExpressionsEnumerator = GetFinder(referenceExpression).GetRelatedExpressions(myScope, referenceExpression).GetEnumerator();

                    if (relatedExpressionsEnumerator.MoveNext())
                    {
                        myReferenceInvalidateBarriers[referenceExpression] = relatedExpressionsEnumerator;
                    }
                    else
                    {
                        myWithoutRelatedExpression.Add(referenceExpression);
                    }
                }

                myContainer.AddProperty(referenceExpression);
            }

            public override void VisitPreprocessorDirective(IPreprocessorDirective preprocessorDirectiveParam)
            {
                InvalidateAll();
            }

            public override void VisitIfStatement(IIfStatement ifStatement)
            {
                InvalidateAll();
                
                var thenBody = ifStatement.Then;
                if (thenBody != null)
                {
                    thenBody.ProcessThisAndDescendants(this);
                    InvalidateAll();
                }

                var elseBody = ifStatement.Else;
                if (elseBody != null)
                {
                    elseBody.ProcessThisAndDescendants(this);
                    InvalidateAll();
                }
            }

            public bool InteriorShouldBeProcessed(ITreeNode element)
            {
                switch (element)
                {
                    case ICSharpClosure _:
                    case IInvocationExpression _:
                    case IAssignmentExpression _:
                    case IIfStatement _:
                    case ILoopStatement _:
                    case ISwitchSection _:
                        return false;
                    default:
                        return true;
                }
            }

            public void ProcessBeforeInterior(ITreeNode element)
            {
                var toRemove = new LocalList <IReferenceExpression>();
                foreach (var (referenceExpression, enumerator) in myReferenceInvalidateBarriers)
                {
                    var current = enumerator.Current.NotNull("current != null");
                    
                    if (element == current)
                    {
                        if (!enumerator.MoveNext())
                        {
                            toRemove.Add(referenceExpression);
                        }

                        myContainer.InvalidateCachedValues(referenceExpression);
                    }
                }

                foreach (var re in toRemove)
                {
                    myReferenceInvalidateBarriers.Remove(re);
                }

                switch (element)
                {
                    case ILoopStatement loopStatement:
                        InvalidateAll();
                        loopStatement.Body.ProcessThisAndDescendants(this);
                        InvalidateAll();
                        break;
                    case ISwitchSection switchSection:
                        InvalidateAll();
                        switchSection.ProcessDescendants(this);
                        InvalidateAll();
                        break;
                    case ISwitchCaseLabel _:
                        InvalidateAll();
                        break;
                    case IGotoStatement _:
                        InvalidateAll();
                        break;
                    case ILabelStatement _:
                        InvalidateAll();
                        break;
                }

                if (element is ICSharpTreeNode sharpNode)
                {
                    sharpNode.Accept(this);
                }
            }

            public override void VisitAssignmentExpression(IAssignmentExpression assignmentExpressionParam)
            {
                assignmentExpressionParam.Source?.ProcessThisAndDescendants(this);
                assignmentExpressionParam.Dest?.ProcessThisAndDescendants(this);
            }

            public override void VisitInvocationExpression(IInvocationExpression invocationExpressionParam)
            {
                invocationExpressionParam.ArgumentList?.ProcessThisAndDescendants(this);
                invocationExpressionParam.InvokedExpression?.ProcessThisAndDescendants(this);
            }

            public void ProcessAfterInterior(ITreeNode element)
            {
            }

            private void InvalidateAll()
            {
                myReferenceInvalidateBarriers.Clear();
                myContainer.InvalidateCachedValues();
            }
            
            public bool ProcessingIsFinished => false;
        } 
    }
}