using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using QualifierEqualityComparer = JetBrains.ReSharper.Psi.CSharp.Impl.ControlFlow.ControlFlowWeakVariableInfo.QualifierEqualityComparer;

namespace JetBrains.ReSharper.Plugins.Unity.Utils
{
        // Note: this class is heuristic for finding related expression for unity component and do not 
        // consider all cases (e.g. ICSharpClosure is ignored, if user assigned reference expression to variable,
        // this variable will not take participate in analysis)
        public class UnityComponentRelatedReferenceExpressionFinder
        {
            protected static readonly QualifierEqualityComparer ReComparer = new QualifierEqualityComparer();
            protected readonly IReferenceExpression ReferenceExpression;
            protected readonly bool myIgnoreNotComponentInvocations;
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

            public IEnumerable<IReferenceExpression> GetRelatedExpressions([NotNull]ITreeNode scope, [CanBeNull] ITreeNode from = null)
            {
                return GetRelatedExpressions(scope, from, from == null);
            }

            private IEnumerable<IReferenceExpression> GetRelatedExpressions([NotNull] ITreeNode scope,
                [CanBeNull] ITreeNode from, bool isFound)
            {
                var descendants = scope.Descendants();

                while (descendants.MoveNext())
                {
                    var current = descendants.Current;
                    if (current == from)
                        isFound = true;
                    switch (current)
                    {
                        case ICSharpClosure _:
                            descendants.SkipThisNode();
                            break;
                        case IInvocationExpression invocationExpression:
                            descendants.SkipThisNode();
                            foreach (var re in GetRelatedExpressions(invocationExpression.ArgumentList, from))
                            {
                                if (isFound)
                                    yield return re;
                            }
                            
                            foreach (var re in GetRelatedExpressions(invocationExpression.InvokedExpression, from))
                            {
                                if (isFound)
                                    yield return re;
                            }
                            
                            continue;
                        case IAssignmentExpression assignmentExpression:
                           descendants.SkipThisNode();
                            foreach (var re in GetRelatedExpressions(assignmentExpression.Dest, from))
                            {
                                if (isFound)
                                    yield return re;
                            }
                            
                            foreach (var re in GetRelatedExpressions(assignmentExpression.Source, from))
                            {
                                if (isFound)
                                    yield return re;
                            }
                            
                            continue;
                        case IReferenceExpression referenceExpression:
                            var currentNodeDeclaredElement = referenceExpression.Reference.Resolve().DeclaredElement as IClrDeclaredElement;
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
                                        if (!myIgnoreNotComponentInvocations && isFound)
                                        {
                                            yield return referenceExpression;
                                        } 
                                        continue;
                                    }
                                    break;
                                default:
                                    continue;
                            }
                            
                            if (isFound && !IsReferenceExpressionNotRelated(referenceExpression, currentNodeDeclaredElement, currentNodeContainingType) )
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