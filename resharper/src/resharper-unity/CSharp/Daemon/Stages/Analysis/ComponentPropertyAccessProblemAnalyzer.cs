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
using JetBrains.ReSharper.Psi.Resolve.Managed;
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
            container.InvalidateCachedValues();
        }

        private void AnalyzeStatements(ICSharpTreeNode scope, PropertiesAccessContainer referencesContainer)
        {
            var visitor = new PropertiesAnalyzerVisitor(referencesContainer, scope, new Dictionary<string, IEnumerator<ITreeNode>>());
            visitor.VisitNode(scope);
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

            // TODO: Which components we should support too?  Uncomment line below to support different components. Do not forget to provide addition filters for new components
            //return containingType.GetSuperTypes().Any(t => t.GetClrName().Equals(KnownTypes.Component));
            return containingType.GetClrName().Equals(KnownTypes.Transform);
        }

        private static IEnumerable<ICSharpTreeNode> FilterWrongRelatedExpression(IEnumerable<ICSharpTreeNode> findRelatedExpressions, IReferenceExpression expression)
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
                if (!(relatedExpression is IReferenceExpression relatedReferenceExpression))
                {
                    yield return relatedExpression;
                    continue;
                }
                var fullRelatedReferenceExpression = ReferenceExpressionNavigator.GetTopByQualifierExpression(relatedReferenceExpression);
                if (filter(fullRelatedReferenceExpression, expression))
                    continue;

                yield return relatedExpression;
            }
        }

        private static bool CommonFilter(ITreeNode node, IReferenceExpression cachedExpression)
        {
            var reLikeString = QualifierToString(cachedExpression);

            IReferenceExpression relatedReferenceExpression = null;
            switch (node)
            {
                case IReferenceExpression referenceExpression:
                    relatedReferenceExpression = referenceExpression;
                    break;
                case IInvocationExpression invocationExpression:
                    relatedReferenceExpression = invocationExpression.InvokedExpression as IReferenceExpression;
                    break;
                default:
                    return false;
            }
            return reLikeString.Equals(QualifierToString(relatedReferenceExpression));
        }

        private static bool TransformFilter(ITreeNode node, IReferenceExpression cachedExpression)
        {
            var cachedPropertyName = cachedExpression.Reference.Resolve().DeclaredElement.NotNull().ShortName;

            IClrDeclaredElement declaredElement = null;
            if (node is IReferenceExpression nodeReferenceExpression)
            {
                declaredElement = nodeReferenceExpression.Reference.Resolve().DeclaredElement as IClrDeclaredElement; 
            }
            else
            {
                return false;
            }

            var containingType = declaredElement?.GetContainingType();
            if (containingType == null)
                return false;

            if (!containingType.GetClrName().Equals(KnownTypes.Transform))
                return true;
            
            if (ourTransformConflicts.ContainsKey(cachedPropertyName))
            {
                var conflicts = ourTransformConflicts[cachedPropertyName];
                return !conflicts.Contains(declaredElement.ShortName);
            }

            return true;
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

            public void InvalidateCachedValues(string name)
            {
                // calculate read/write operations for property
                int write = 0;
                int read = 0;
                var highlighitingElements = myPropertiesMap[name].ToArray();

                int startHighlightIndex = -1;
                for (int i = 0; i < highlighitingElements.Length; i++)
                {
                    var referenceExpression = ReferenceExpressionNavigator.GetTopByQualifierExpression(highlighitingElements[i]);
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
                        myConsumer.AddHighlighting(new InefficientPropertyAccessWarning(highlighitingElements[i], highlighitingElements, read > 0, write > 0));
                    }
                }

                myPropertiesMap.RemoveKey(name);
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

        private class PropertiesAnalyzerVisitor : TreeNodeVisitor
        {
            private readonly PropertiesAccessContainer myContainer;
            private readonly ICSharpTreeNode myScope;
            private readonly Dictionary<string, IEnumerator<ITreeNode>> myReferenceInvalidateBarriers;

            public PropertiesAnalyzerVisitor(PropertiesAccessContainer container, ICSharpTreeNode scope, Dictionary<string, IEnumerator<ITreeNode>> referenceInvalidateBarriers)
            {
                myContainer = container;
                myScope = scope;
                myReferenceInvalidateBarriers = referenceInvalidateBarriers;
            }

            public override void VisitNode(ITreeNode node)
            {
                var toRemove = new List<string>();
                foreach (var (referenceString, enumerator) in myReferenceInvalidateBarriers)
                {
                    var current = enumerator.Current;
                    if (node == current)
                    {
                        var hasNext = enumerator.MoveNext();
                        if (!hasNext)
                            toRemove.Add(referenceString);
                        myContainer.InvalidateCachedValues(referenceString);
                    }
                }
                toRemove.ForEach(t => myReferenceInvalidateBarriers.Remove(t));

                switch (node)
                {
                    case ILoopStatement loopStatement:
                        myContainer.InvalidateCachedValues();
                        var loopBody = loopStatement.Body;
                        if (loopBody != null)
                        {
                            VisitNode(loopBody);
                            myContainer.InvalidateCachedValues();
                        }
                        return;
                    case IIfStatement ifStatement:
                        myContainer.InvalidateCachedValues();
                        var thenBody = ifStatement.Then;
                        var elseBody = ifStatement.Else;

                        if (thenBody != null)
                        {
                            VisitNode(thenBody);
                            myContainer.InvalidateCachedValues();
                        }
                        if (elseBody != null)
                        {
                            VisitNode(elseBody);
                            myContainer.InvalidateCachedValues();
                        }
                        return;
                     case ISwitchSection switchSections:
                         myContainer.InvalidateCachedValues();
                         return;
                    case IReferenceExpression referenceExpression:
                        referenceExpression.Accept(this);
                        return;
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

            public override void VisitReferenceExpression([NotNull] IReferenceExpression referenceExpression)
            {
                var qualifier = referenceExpression.QualifierExpression;
                if (qualifier != null)
                {
                    VisitNode(qualifier);
                }
                if (!IsUnityComponentProperty(referenceExpression))
                    return;
                
                var referenceExpressionAsString = QualifierToString(referenceExpression);
                if (referenceExpressionAsString == null)
                    return;
                
                if (!myReferenceInvalidateBarriers.ContainsKey(referenceExpressionAsString))
                {
                    var checker = ExpressionWriteAccessChecker.CreateAccessCheckerForExpression(referenceExpression);
                    var relatedExpressions = FilterWrongRelatedExpression(checker.FindRelatedExpressions(myScope), referenceExpression);
                    var relatedExpressionsEnumerator = relatedExpressions.GetEnumerator();

                    if (relatedExpressionsEnumerator.MoveNext())
                        myReferenceInvalidateBarriers[referenceExpressionAsString] = relatedExpressionsEnumerator;
                }

                myContainer.AddProperty(referenceExpressionAsString, referenceExpression);
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

        #region TransformPropertiesConflicts

        // Short name of transform property to short name of method or properties which get change source property.
        // If this map do not contain transform property, there is no conflicts for this property
        private static readonly Dictionary<string, ISet<string>> ourTransformConflicts = new Dictionary<string, ISet<string>>()
        {
            {"position", new HashSet<string>()
                {
                    "localPosition",
                    "parent",
                    "SetParent",
                    "SetPositionAndRotation",
                    "Translate",
                }
            },
            {"localPosition", new HashSet<string>()
                {
                    "position",
                    "parent",
                    "SetParent",
                    "SetPositionAndRotation",
                    "Translate",
                }
            },
            {"eulerAngles", new HashSet<string>()
                {
                    "localEulerAngles",
                    "rotation",
                    "localRotation",
                    "parent",
                    "SetParent",
                    "SetPositionAndRotation",
                    "Rotate",
                    "RotateAround",
                    "LookAt",
                    "RotateAroundLocal"
                }
            },
            {"localEulerAngles", new HashSet<string>()
                {
                    "eulerAngles",
                    "rotation",
                    "localRotation",
                    "parent",
                    "SetParent",
                    "SetPositionAndRotation",
                    "Rotate",
                    "RotateAround",
                    "LookAt",
                    "RotateAroundLocal"
                }
            },
            {"rotation", new HashSet<string>()
                {
                    "eulerAngles",
                    "eulerAngles",
                    "localRotation",
                    "parent",
                    "SetParent",
                    "SetPositionAndRotation",
                    "Rotate",
                    "RotateAround",
                    "LookAt",
                    "RotateAroundLocal"
                }
            },
            {"localRotation", new HashSet<string>()
                {
                    "eulerAngles",
                    "eulerAngles",
                    "rotation",
                    "parent",
                    "SetParent",
                    "SetPositionAndRotation",
                    "Rotate",
                    "RotateAround",
                    "LookAt",
                    "RotateAroundLocal"
                }
            },
            {"localScale", new HashSet<string>()
                {
                    "parent",
                    "SetParent",
                    "lossyScale"
                }
            },
            {"lossyScale", new HashSet<string>()
                {
                    "parent",
                    "SetParent",
                    "scale"
                }
            }
        };

        #endregion
    }
}