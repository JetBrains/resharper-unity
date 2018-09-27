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
using QualifierEqualityComparer = JetBrains.ReSharper.Psi.CSharp.Impl.ControlFlow.ControlFlowWeakVariableInfo.QualifierEqualityComparer;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Analysis
{
    [ElementProblemAnalyzer(
        ElementTypes: new [] {typeof(IMethodDeclaration), typeof(ICSharpClosure)}, 
        HighlightingTypes = new[] {typeof(InefficientPropertyAccessWarning)})]
    public class ComponentPropertyAccessProblemAnalyzer : UnityElementProblemAnalyzer<ITreeNode>
    {
        private static readonly QualifierEqualityComparer ourComparer = new QualifierEqualityComparer();
        
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
            var declaredElement = (referenceExpression.Reference.Resolve().DeclaredElement as IClrDeclaredElement).NotNull("declaredElement != null");
            var containingType = declaredElement.GetContainingType().NotNull("declaredElement.GetContainingType() != null");

            if (containingType.GetClrName().Equals(KnownTypes.Transform))
                return new TransformRelatedReferenceFinder(referenceExpression);
            
            return new UnityComponentRelatedReferenceExpressionFinder(referenceExpression);
        }
        
        private class PropertiesAccessContainer
        {
            private readonly IHighlightingConsumer myConsumer;
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
                    var referenceExpression = ReferenceExpressionNavigator.GetTopByQualifierExpression(highlighitingElements[i])
                        .NotNull("referenceExpression != null");
                    var assignmentExpression = AssignmentExpressionNavigator.GetByDest(referenceExpression.GetOperandThroughParenthesis());
                    
                    if (read == 0 && write == 0)
                    {
                        readAnchor = referenceExpression.GetContainingStatement().NotNull("readAnchor != null");
                        var relatedExpressions = GetFinder(referenceExpression).GetRelatedExpressions(readAnchor);
                        // if we have related expression before considered reference expression, we can only inline reading into statement
                        // Example:
                        // transform.Position = (transform.localPosition = Vector3.Up) + transform.position + transform.position;
                        //                       ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                        // transform.localPosition is related to transform.position, but we need cache our variable
                        // The result is:
                        // var cache = transform.position;
                        // transform.position = (transform.localPosition = Vector3.Up) + (cache = transform.position) + cache;

                        var first = relatedExpressions.FirstOrDefault();
                        if (first != null)
                        {
                            inlineCacheValue = first.GetTreeStartOffset() < referenceExpression.GetTreeStartOffset();
                        }
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
                        var relatedExpressions = GetFinder(lastWriteExpression).GetRelatedExpressions(writeAnchor);
                        var last = relatedExpressions.LastOrDefault();
                        if (last != null)
                        {
                            inlineRestoreValue = last.GetTreeStartOffset() > lastWriteExpression .GetTreeStartOffset();
                        }
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
                var qualifier = referenceExpression.QualifierExpression;

                if (!IsUnityComponentProperty(referenceExpression))
                    return;

                if (!IsReferenceExpressionOnly(referenceExpression))
                    return;
                
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
                myContainer.InvalidateCachedValues();
            }

            public override void VisitIfStatement(IIfStatement ifStatement)
            {
                myContainer.InvalidateCachedValues();
                
                var thenBody = ifStatement.Then;
                if (thenBody != null)
                {
                    thenBody.ProcessThisAndDescendants(this);
                    myContainer.InvalidateCachedValues();
                }

                var elseBody = ifStatement.Else;
                if (elseBody != null)
                {
                    elseBody.ProcessThisAndDescendants(this);
                    myContainer.InvalidateCachedValues();
                }
            }

            public bool InteriorShouldBeProcessed(ITreeNode element)
            {
                switch (element)
                {
                    case ICSharpClosure _:
                        return false;
                    case IIfStatement _:
                        return false;
                    case ILoopStatement _:
                        return false;
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
                    while (current.GetTreeStartOffset() < element.GetTreeStartOffset())
                    {
                        var hasNext = enumerator.MoveNext();
                        if (!hasNext)
                        {
                            toRemove.Add(referenceExpression);
                            break;
                        }

                        current = enumerator.Current.NotNull("current != null");
                    }
                    
                    if (element == current)
                    {
                        var hasNext = enumerator.MoveNext();
                        if (!hasNext)
                            toRemove.Add(referenceExpression);
                        myContainer.InvalidateCachedValues(referenceExpression);
                    }
                }
                toRemove.ForEach(t => myReferenceInvalidateBarriers.Remove(t));

                switch (element)
                {
                    case ILoopStatement loopStatement:
                        myContainer.InvalidateCachedValues();
                        loopStatement.Body.ProcessThisAndDescendants(this);
                        myContainer.InvalidateCachedValues();
                        break;
                    case ISwitchSection switchSection:
                        myContainer.InvalidateCachedValues();
                        switchSection.ProcessDescendants(this);
                        myContainer.InvalidateCachedValues();
                        break;
                }
                
                if (element is ICSharpTreeNode sharpNode)
                {
                    sharpNode.Accept(this);
                }
            }

            public void ProcessAfterInterior(ITreeNode element)
            {
            }

            public bool ProcessingIsFinished => false;
        }

        // Note: this class is heuristic for finding related expression for unity component and do not 
        // consider all cases (e.g. ICSharpClosure is ignored, if user assigned reference expression to variable,
        // this variable will not take participate in analysis)
        public class UnityComponentRelatedReferenceExpressionFinder
        {
            protected static readonly QualifierEqualityComparer ReComparer = new QualifierEqualityComparer();
            protected readonly IReferenceExpression ReferenceExpression;
            private readonly bool myIgnoreNotComponentInvocations;
            protected readonly IReferenceExpression ComponentReferenceExpression;
            protected readonly ITypeElement ContainingType;
            protected readonly IClrDeclaredElement DeclaredElement;
            
            
            public UnityComponentRelatedReferenceExpressionFinder([NotNull]IReferenceExpression referenceExpression, bool ignoreNotComponentInvocations = false)
            {
                ReferenceExpression = referenceExpression;
                myIgnoreNotComponentInvocations = ignoreNotComponentInvocations;

                DeclaredElement = ReferenceExpression.Reference.Resolve().DeclaredElement as IClrDeclaredElement;
                Assertion.Assert(DeclaredElement != null, "DeclaredElement != null");
                    
                ContainingType = DeclaredElement.GetContainingType();
                Assertion.Assert(ContainingType != null, "ContainingType != null");
                
                ComponentReferenceExpression = referenceExpression.QualifierExpression as IReferenceExpression;
                Assertion.Assert(ComponentReferenceExpression != null, "ComponentReferenceExpression != null");
            }

            public IEnumerable<IReferenceExpression> GetRelatedExpressions([NotNull]ITreeNode scope)
            {
                var descendants = scope.Descendants();

                while (descendants.MoveNext())
                {
                    var current = descendants.Current;

                    switch (current)
                    {
                        case ICSharpClosure _:
                            descendants.SkipThisNode();
                            break;
                        case IReferenceExpression referenceExpression:
                            var currentNodeDeclaredElement = referenceExpression.Reference?.Resolve().DeclaredElement as IClrDeclaredElement;
                            var currentNodeContainingType = currentNodeDeclaredElement?.GetContainingType();
                            switch (currentNodeDeclaredElement)
                            {
                                case IField _:
                                case IProperty _:
                                    var qualifier = referenceExpression.QualifierExpression as IReferenceExpression;
                                    if (qualifier == null)
                                        continue;
                                    
                                    if (currentNodeContainingType == null)
                                        continue;

                                    if (!ContainingType.Equals(currentNodeContainingType))
                                        continue;
                                    
                                    if (!ReComparer.Equals(ComponentReferenceExpression, qualifier))
                                        continue;
                                    
                                    break;
                                case IMethod method:
                                    if (currentNodeContainingType == null ||
                                        !ContainingType.Equals(currentNodeContainingType))
                                    {
                                        if (!myIgnoreNotComponentInvocations)
                                        {
                                            yield return referenceExpression;
                                        } 
                                        continue;
                                    }
                                    break;
                                default:
                                    continue;
                            }
                            
                            if (!IsReferenceExpressionNotRelated(referenceExpression, currentNodeDeclaredElement, currentNodeContainingType))
                                yield return referenceExpression;
                            
                            break;
                    }
                }
            }

            protected virtual bool IsReferenceExpressionNotRelated([NotNull]IReferenceExpression currentReference, 
                IClrDeclaredElement currentElement, ITypeElement currentContainingType)
            {
                return ReComparer.Equals(currentReference, ReferenceExpression);
            }
        }

        public class TransformRelatedReferenceFinder : UnityComponentRelatedReferenceExpressionFinder
        {
            public TransformRelatedReferenceFinder([NotNull] IReferenceExpression referenceExpression)
                : base(referenceExpression, true)
            {
            }

            protected override bool IsReferenceExpressionNotRelated([NotNull] IReferenceExpression currentReference, 
                IClrDeclaredElement currentElement, ITypeElement currentContainingType)
            {
                if (base.IsReferenceExpressionNotRelated(currentReference, currentElement, currentContainingType))
                    return true;

                if (!currentContainingType.GetClrName().Equals(KnownTypes.Transform))
                    return true;
            
                if (ourTransformConflicts.ContainsKey(DeclaredElement.ShortName))
                {
                    var conflicts = ourTransformConflicts[DeclaredElement.ShortName];
                    return !conflicts.Contains(currentElement.ShortName);
                }

                return true;
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
                        "localEulerAngles",
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
                        "localEulerAngles",
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
                },
                {"right", new HashSet<string>()
                    {
                        "eulerAngles",
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
                {"up", new HashSet<string>()
                    {
                        "eulerAngles",
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
                {"forward", new HashSet<string>()
                    {
                        "eulerAngles",
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
                } ,
                {"parent", new HashSet<string>()
                    {
                        "eulerAngles",
                        "localEulerAngles",
                        "rotation",
                        "localRotation",
                        "SetPositionAndRotation",
                        "Rotate",
                        "RotateAround",
                        "LookAt",
                        "RotateAroundLocal",
                        "position",
                        "localPosition",
                        "Translate",
                    }
                } 
            };
    
            #endregion
        }
        
      
    }
}