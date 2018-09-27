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
using JetBrains.Util;
using QualifierEqualityComparer = JetBrains.ReSharper.Psi.CSharp.Impl.ControlFlow.ControlFlowWeakVariableInfo.QualifierEqualityComparer;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(
        ElementTypes: new [] {typeof(IMethodDeclaration), typeof(ICSharpClosure)}, 
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
            IBlock block = null;
            
            switch (node)
            {
                case IMethodDeclaration methodDeclaration:
                    block = methodDeclaration.Body;
                    break;
                case ICSharpClosure closure:
                    var body = closure.GetCodeBody();
                    var exprBody = body.ExpressionBody;
                    if (exprBody != null)
                    {
                        AnalyzeStatements(exprBody, container);
                    }
                    else
                    {
                        block = body.BlockBody;
                    }
                    break;
            }
            
            if (block != null)
            {
                if (block.Descendants<IGotoStatement>().Any())
                    return;
                    
                AnalyzeStatements(block, container);
            }
            
            // cache all references which is not invalidated dut to no related expression was found
            container.InvalidateCachedValues();
        }

        private void AnalyzeStatements(ICSharpTreeNode scope, PropertiesAccessContainer referencesContainer)
        {
            var visitor = new PropertiesAnalyzerVisitor(referencesContainer, scope, new Dictionary<IReferenceExpression, IEnumerator<ITreeNode>>(ourComparer));
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

            return containingType.GetSuperTypes().Any(t => t.GetClrName().Equals(KnownTypes.Component));
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
                
                var highlighitingElements = myPropertiesMap[key].ToArray();
                Assertion.Assert(highlighitingElements.Length > 0, "highlighitingElements.Length > 0");
                
                // calculate read/write operations for property
                int write = 0;
                int read = 0;
                int startHighlightIndex = -1;

                ICSharpStatement readAnchor = null;
                bool inlineCacheValue = false;
                ICSharpStatement writeAnchor = null;
                IReferenceExpression lastWriteExpression = null;
                bool inlineRestoreValue = false;
                
                for (int i = 0; i < highlighitingElements.Length; i++)
                {
                    var referenceExpression = highlighitingElements[i];
                    var fullReferenceExpression = ReferenceExpressionNavigator.GetTopByQualifierExpression(highlighitingElements[i])
                        .NotNull("referenceExpression != null");
                    var assignmentExpression = AssignmentExpressionNavigator.GetByDest(fullReferenceExpression.GetOperandThroughParenthesis());
                    
                    if (read == 0 && write == 0)
                    {
                        readAnchor = referenceExpression.GetContainingStatement().NotNull("readAnchor != null");
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
                    
                    if (assignmentExpression != null)
                    {
                        write++;
                        lastWriteExpression = referenceExpression;
                        writeAnchor = assignmentExpression.GetContainingStatement().NotNull("writeAnchor != null");
                    }
                    if (assignmentExpression != null && assignmentExpression.IsCompoundAssignment || assignmentExpression == null)
                    {
                        read++;
                    }


                    if (startHighlightIndex == -1 && (read == 2|| write == 2 | read + write == 3))
                    {
                        startHighlightIndex = i;
                    }
                }

                if (writeAnchor != null)
                {
                    if (writeAnchor is IReturnStatement)
                    {
                        inlineRestoreValue = true;
                    } else {
                        var relatedExpressions = GetFinder(lastWriteExpression).GetRelatedExpressions(writeAnchor, lastWriteExpression);
                        inlineRestoreValue = relatedExpressions.Count() > 0;
                    }
                }

                if (startHighlightIndex != -1)
                {
                    for (int i = startHighlightIndex; i < highlighitingElements.Length; i++)
                    {
                        var warning = new InefficientPropertyAccessWarning(highlighitingElements[i], highlighitingElements, 
                            readAnchor, inlineCacheValue, writeAnchor, inlineRestoreValue);
                        myConsumer.AddHighlighting(warning);
                    }
                }

                myPropertiesMap.Remove(key);
            }

            private ITreeNode GetPreviousRelatedExpression(IReferenceExpression referenceExpression, ICSharpStatement readAnchor)
            {
                var finder = GetFinder(referenceExpression);
                var stopElement = finder.GetRelatedExpressions(readAnchor).FirstOrDefault();
                var fromSequence = finder.GetRelatedExpressions(readAnchor, referenceExpression);

                ITreeNode prev = null;
                foreach (var expression in fromSequence)
                {
                    if (expression == stopElement)
                        return prev;
                    prev = expression;
                }

                return null;
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

            public PropertiesAnalyzerVisitor(PropertiesAccessContainer container, ICSharpTreeNode scope, Dictionary<IReferenceExpression, IEnumerator<ITreeNode>> referenceInvalidateBarriers)
            {
                myContainer = container;
                myScope = scope;
                myReferenceInvalidateBarriers = referenceInvalidateBarriers;
            }

            public override void VisitReferenceExpression([NotNull] IReferenceExpression referenceExpression)
            {
                if (!IsUnityComponentProperty(referenceExpression))
                    return;

                if (!IsReferenceExpressionOnly(referenceExpression))
                    return;
                
                // first encounter of reference. Create group and find related expressions.
                if (!myReferenceInvalidateBarriers.ContainsKey(referenceExpression))
                {
                    var relatedExpressionsEnumerator = GetFinder(referenceExpression).GetRelatedExpressions(myScope).GetEnumerator();

                    if (relatedExpressionsEnumerator.MoveNext())
                        myReferenceInvalidateBarriers[referenceExpression] = relatedExpressionsEnumerator;
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
                var toRemove = new List<IReferenceExpression>();
                foreach (var (referenceExpression, enumerator) in myReferenceInvalidateBarriers)
                {
                    var current = enumerator.Current.NotNull("current != null");
                    
                    if (element == current)
                    {
                        var hasNext = enumerator.MoveNext();
                        if (!hasNext)
                            toRemove.Add(referenceExpression);
                        Invalidate(referenceExpression);
                    }
                }
                toRemove.ForEach(t => myReferenceInvalidateBarriers.Remove(t));

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
                }
                
                if (element is ICSharpTreeNode sharpNode)
                {
                    sharpNode.Accept(this);
                }
            }

            public override void VisitAssignmentExpression(IAssignmentExpression assignmentExpressionParam)
            {
                assignmentExpressionParam.Source.ProcessThisAndDescendants(this);
                assignmentExpressionParam.Dest.ProcessThisAndDescendants(this);
            }

            public override void VisitInvocationExpression(IInvocationExpression invocationExpressionParam)
            {
                invocationExpressionParam.ArgumentList.ProcessThisAndDescendants(this);
                invocationExpressionParam.InvokedExpression.ProcessThisAndDescendants(this);
            }

            public void ProcessAfterInterior(ITreeNode element)
            {
            }

            private void Invalidate(IReferenceExpression referenceExpression)
            {
                myContainer.InvalidateCachedValues(referenceExpression);
                myReferenceInvalidateBarriers.Remove(referenceExpression);
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