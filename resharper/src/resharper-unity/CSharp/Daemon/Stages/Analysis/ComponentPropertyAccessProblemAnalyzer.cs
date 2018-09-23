using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.CSharp.Util;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Dispatcher;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.PsiGen.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(
        ElementTypes: new [] {typeof(IMethodDeclaration), typeof(ICSharpClosure)}, 
        HighlightingTypes = new[] {typeof(InefficientPropertyAccessWarning)})]
    public class ComponentPropertyAccessProblemAnalyzer : UnityElementProblemAnalyzer<ITreeNode>
    {

        private delegate bool TreeNodeFilter(ITreeNode toFilter, IReferenceExpression cachedExpression);
        
        private static readonly IDictionary<IClrTypeName, TreeNodeFilter> ourFilters 
            = new Dictionary<IClrTypeName, TreeNodeFilter>()
            {
                {KnownTypes.Transform, TransformFilter}
            };
        
        public ComponentPropertyAccessProblemAnalyzer([NotNull] UnityApi unityApi)
            : base(unityApi)
        {
        }
        
        protected override void Analyze(ITreeNode node, ElementProblemAnalyzerData data, IHighlightingConsumer consumer)
        {   
            var container = new PropertiesAccessContainer(consumer);
            var visitor = new PropertyAccessProblemVisitor(container);
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
                        exprBody.Accept(visitor);
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
                    
                AnalyzeStatements(block, visitor);
            }
            container.InvalidateCachedValues();
        }

        private void AnalyzeStatements(IBlock block, TreeNodeVisitor visitor)
        {
            var referenceInvalidateBarriers = new Dictionary<string, IEnumerator<ITreeNode>>();
            var referencesContainer = new OneToListMap<string, IReferenceExpression>();

            var nodeIterator = block.Descendants();

            while (nodeIterator.MoveNext())
            {
                switch (nodeIterator.Current)
                {
                    case ICSharpClosure _:
                        nodeIterator.SkipThisNode();
                        break;
                    case IReferenceExpression referenceExpression:
                        if (!IsUnityComponentProperty(referenceExpression))
                            break;

                        var referenceExpressionAsString = QualifierToString(referenceExpression);
                        if (!referenceInvalidateBarriers.ContainsKey(referenceExpressionAsString))
                        {
                            var checker = ExpressionWriteAccessChecker.CreateAccessCheckerForExpression(referenceExpression);
                            var relatedExpressions = FilterWrongRelatedExpression(checker.FindRelatedExpressions(block), referenceExpression);
                            var relatedExpressionsEnumerator = relatedExpressions.GetEnumerator();
                            
                            if (relatedExpressionsEnumerator.MoveNext())
                                referenceInvalidateBarriers[referenceExpressionAsString] = relatedExpressionsEnumerator;
                        }
                        referencesContainer[referenceExpressionAsString].Add(referenceExpression);
                        
                        goto default;
                        break;
                    default:
                        var toRemove = new List<string>();
                        foreach (var (referenceString, enumerator) in referenceInvalidateBarriers)
                        {
                            var current = enumerator.Current;
                            if (nodeIterator.Current == current)
                            {
                                var hasNext = enumerator.MoveNext();
                                if (!hasNext)
                                    toRemove.Add(referenceString);
                                InvalidateCache(referencesContainer, referenceString);
                            }
                        }
                        toRemove.ForEach(t => referenceInvalidateBarriers.Remove(t));
                        break;
                }

                InvalidateCache(referencesContainer);
            }
        }

        private void InvalidateCache(OneToListMap<string, IReferenceExpression> referencesContainer, string toInvalidateCacheReferenceName)
        {
            //throw new System.NotImplementedException();
        }

        
        private void InvalidateCache(OneToListMap<string, IReferenceExpression> referencesContainer)
        {
            //    throw new System.NotImplementedException();
        }

        private bool IsUnityComponentProperty(IReferenceExpression referenceExpression)
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

        private IEnumerable<ICSharpTreeNode> FilterWrongRelatedExpression(IEnumerable<ICSharpTreeNode> findRelatedExpressions, IReferenceExpression expression)
        {
            var declaredElement = (expression.Reference.Resolve().DeclaredElement as IClrDeclaredElement).NotNull("declaredElement != null");
            var clrType = declaredElement.GetContainingType().NotNull("declaredElement.GetContainingType() != null").GetClrName();
            
            TreeNodeFilter filter;
            if (ourFilters.ContainsKey(clrType))
            {
                filter = ourFilters[clrType];
            }
            else
            {
                filter = CommonFilter;
            }

            foreach (var relatedExpression in findRelatedExpressions)
            {
                if (filter(relatedExpression, expression))
                    continue;

                yield return relatedExpression;
            }
        }

        private bool CommonFilter(ITreeNode node, IReferenceExpression cachedExpression)
        {
            var reLikeString = QualifierToString(cachedExpression);
            if (!(node is IReferenceExpression relatedReferenceExpression))
            {
                return false;
            }

            return reLikeString.Equals(QualifierToString(relatedReferenceExpression));
        }

        private static bool TransformFilter(ITreeNode node, IReferenceExpression cachedExpression)
        {
            var cachedPropertyName = cachedExpression.Reference.Resolve().DeclaredElement.NotNull().ShortName;
            
            switch (node)
            {
                case IInvocationExpression invocation:
                    var method = invocation.Reference?.Resolve().DeclaredElement as IMethod;
                    if (method == null)
                        return false;

                    var shortName = method.ShortName;
                    
                    break;
                case IReferenceExpression referenceExpression:
                    break;
                default:
                    return true;
            }

            return false;
        }

        private class PropertyAccessProblemVisitor : TreeNodeVisitor
        {
            private readonly PropertiesAccessContainer myContainer;

            public PropertyAccessProblemVisitor(PropertiesAccessContainer container)
            {
                myContainer = container;
            }

            public override void VisitNode(ITreeNode node)
            {
                switch (node)
                {
                    case ILoopStatement loop:
                        // like if statement
                        myContainer.InvalidateCachedValues();
                        loop.Body?.Accept(this);
                        return;
                    case ICSharpClosure _:
                        return;
                    case IInvocationExpression invocation:
                        VisitInvocationExpression(invocation);
                        return;
                    case IReferenceExpression expression:
                        VisitReferenceExpression(expression);
                        return;
                    case IAssignmentExpression assignmentExpression:
                        VisitAssignmentExpression(assignmentExpression);
                        return;
                    case IIfStatement ifStatement:
                        VisitIfStatement(ifStatement);
                        return;
                    case ISwitchSection switchSection:
                        myContainer.InvalidateCachedValues();
                        break;
                    case IPreprocessor preprocessor:
                        // hard to handle branches
                        myContainer.InvalidateCachedValues();
                        break;
                }
                
                foreach (var children in node.Children())
                {
                    VisitNode(children);
                }
            }

            public override void VisitAssignmentExpression(IAssignmentExpression assignmentExpressionParam)
            {
                // correct order of children
                
                VisitNode(assignmentExpressionParam.Source);
                VisitNode(assignmentExpressionParam.Dest);
            }

            public override void VisitIfStatement([NotNull] IIfStatement ifStatement)
            {
                // We don't support branches in control flow graph due to some problems:
                // 1) If first branch invalidates cache value, where should we invalidate this cache in the second branch?
                // 2) What if depth greater than one?
                // Cases when we should cache value in different branches is rarely, but code base will be big for support this situation
                myContainer.InvalidateCachedValues();

                ifStatement.Then?.Accept(this);
                myContainer.InvalidateCachedValues();

                ifStatement.Else?.Accept(this);
                myContainer.InvalidateCachedValues();
            }

            public override void VisitReferenceExpression([NotNull] IReferenceExpression referenceExpression)
            {
                var qualifier = referenceExpression.QualifierExpression;
                if (qualifier != null)
                {
                    VisitNode(qualifier);
                }

                var info = referenceExpression.Reference.Resolve();
                if (info.ResolveErrorType != ResolveErrorType.OK)
                    return;
    
                var property = info.DeclaredElement as IProperty;
                var containingType = property?.GetContainingType();
                if (containingType == null) 
                    return;
                
                if (!containingType.GetSuperTypes().Any(t => t.GetClrName().Equals(KnownTypes.Component))) 
                    return;

                var name = QualifierToString(referenceExpression);
                if (name != null)
                    myContainer.AddProperty(name, referenceExpression);
            }

            public override void VisitInvocationExpression(IInvocationExpression invocationExpressionParam)
            {
                // method can wait for new value of component property
                
                VisitNode(invocationExpressionParam.InvokedExpression);
                invocationExpressionParam.ArgumentList.Accept(this);

                // heuristics : expect that unity methods excluding methods of component do not wait for updated properties values, so do not invalidate cache
                var info = invocationExpressionParam.Reference?.Resolve();
                if (info?.ResolveErrorType == ResolveErrorType.OK)
                {
                    var method = (info.DeclaredElement as IMethod).NotNull("info.DeclaredElement as IMethod != null");
                    var containingType = method.GetContainingType();
                    if (containingType != null && containingType.GetClrName().FullName.StartsWith("UnityEngine.") && 
                        containingType.GetSuperTypes().All(t => !t.GetClrName().Equals(KnownTypes.Component)))
                    {
                        return;
                    }
                }
                myContainer.InvalidateCachedValues();
            }
        }

        private class PropertiesAccessContainer
        {
            private readonly IHighlightingConsumer myConsumer;
            private readonly OneToListMap<string, IReferenceExpression> myPropertiesMap = new OneToListMap<string, IReferenceExpression>();

            public PropertiesAccessContainer(IHighlightingConsumer consumer)
            {
                myConsumer = consumer;
            }

            public void AddProperty(string name, IReferenceExpression referenceExpression)
            {
                myPropertiesMap.Add(name, referenceExpression);;
            }

            public void InvalidateCachedValues()
            {
                foreach (var kvp in myPropertiesMap)
                {
                    // calculate read/write operations for property
                    int write = 0;
                    int read = 0;
                    var highlighitingElements = kvp.Value.ToArray();

                    int startHighlightIndex = -1;
                    for (int i = 0; i < highlighitingElements.Length; i++)
                    {
                        var referenceExpression = highlighitingElements[i];
                        var assignmentExpression = AssignmentExpressionNavigator.GetByDest(referenceExpression.GetOperandThroughParenthesis());
                        if (assignmentExpression != null)
                        {
                            write++;
                            if (assignmentExpression.IsCompoundAssignment)
                            {
                                read++;
                            }
                        }
                        else
                        {
                            read++;
                        }

                        if (startHighlightIndex == -1 && (read == 2|| write == 2 | read + write == 3))
                        {
                            startHighlightIndex = i;
                        }
                    }

                    if (startHighlightIndex != -1)
                    {
                        for (int i = startHighlightIndex; i < highlighitingElements.Length; i++)
                        {
                            myConsumer.AddHighlighting(new InefficientPropertyAccessWarning(highlighitingElements[i], highlighitingElements, true));
                        }
                    }
                }

                myPropertiesMap.Clear();
            }
        } 

        private static string QualifierToString(IReferenceExpression referenceExpression)
        {
            var sb = new StringBuilder();

            var elements = new Stack<IReference>();
            elements.Push(referenceExpression.Reference);

            var qualifier = referenceExpression.QualifierExpression;
            while (qualifier != null)
            {
                if (!(qualifier is IReferenceExpression re))
                {
                    return null;
                }

                qualifier = re.QualifierExpression;
                elements.Push(re.Reference);
            }

            foreach (var e in elements)
            {
                sb.Append(e.GetName());
                sb.Append(".");
            }

            sb.Remove(sb.Length - 1, 1);

            return sb.ToString();
        }
    }
}