using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis
{
    internal static class PerformanceCriticalCodeStageUtil
    {
        public static bool IsInvokedElementExpensive([CanBeNull] IMethod method)
        {
            var containingType = method?.GetContainingType();

            if (containingType == null)
                return false;

            ISet<string> knownCostlyMethods = null;
            var clrTypeName = containingType.GetClrName();

            if (clrTypeName.Equals(KnownTypes.Component))
                knownCostlyMethods = ourKnownComponentCostlyMethods;

            if (clrTypeName.Equals(KnownTypes.MonoBehaviour))
                knownCostlyMethods = ourKnownMonoBehaviourCostlyMethods;

            if (clrTypeName.Equals(KnownTypes.GameObject))
                knownCostlyMethods = ourKnownGameObjectCostlyMethods;

            if (clrTypeName.Equals(KnownTypes.Resources))
                knownCostlyMethods = ourKnownResourcesCostlyMethods;

            if (clrTypeName.Equals(KnownTypes.Object))
                knownCostlyMethods = ourKnownObjectCostlyMethods;

            if (clrTypeName.Equals(KnownTypes.Transform))
                knownCostlyMethods = ourKnownTransformCostlyMethods;

            if (clrTypeName.Equals(KnownTypes.Debug))
                knownCostlyMethods = ourKnownDebugCostlyMethods;

            var shortName = method.ShortName;

            if (knownCostlyMethods != null && knownCostlyMethods.Contains(shortName))
                return true;

            return clrTypeName.Equals(KnownTypes.GameObject) && shortName.Equals("AddComponent");
        }

        public static bool IsInvocationExpensive([NotNull] IInvocationExpression invocationExpression)
        {
            invocationExpression.GetPsiServices().Locks.AssertReadAccessAllowed();

            var reference = (invocationExpression.InvokedExpression as IReferenceExpression)?.Reference;

            if (reference == null)
                return false;

            var declaredElement = reference.Resolve().DeclaredElement as IMethod;

            return IsInvokedElementExpensive(declaredElement);
        }

        public static bool IsCameraMainUsage(IReferenceExpression referenceExpression)
        {
            if (referenceExpression.NameIdentifier?.Name == "main")
            {
                var info = referenceExpression.Reference.Resolve();
                if (info.ResolveErrorType == ResolveErrorType.OK)
                {
                    var property = info.DeclaredElement as IProperty;
                    var containingType = property?.GetContainingType();
                    if (containingType != null && KnownTypes.Camera.Equals(containingType.GetClrName()))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsNullComparisonWithUnityObject(IEqualityExpression equalityExpression, out string possibleName)
        {
            possibleName = null;
            var reference = equalityExpression.Reference;
            if (reference == null)
                return false;

            var isNullFound = false;
            var leftOperand = equalityExpression.LeftOperand;
            var rightOperand = equalityExpression.RightOperand;

            if (leftOperand == null || rightOperand == null)
                return false;

            ICSharpExpression expression = null;

            if (leftOperand.ConstantValue.IsNull())
            {
                isNullFound = true;
                expression = rightOperand;
            }
            else if (rightOperand.ConstantValue.IsNull())
            {
                isNullFound = true;
                expression = leftOperand;
            }

            if (!isNullFound)
                return false;

            var typeElement = expression.GetExpressionType().ToIType()?.GetTypeElement();
            if (typeElement == null)
                return false;

            if (typeElement.GetAllSuperTypes().Any(t => t.GetClrName().Equals(KnownTypes.Object)))
            {

                var suffix = equalityExpression.EqualityType == EqualityExpressionType.NE ? "NotNull" : "Null";

                string baseName;
                if (expression is IReferenceExpression referenceExpression)
                {
                    baseName = referenceExpression.NameIdentifier.Name;
                }
                else
                {
                    baseName = typeElement.ShortName;
                }

                possibleName  = "is" + baseName + suffix;
                return true;
            }

            return false;
        }
        
        public static bool IsPerformanceCriticalRootMethod([CanBeNull] ITreeNode node)
        {
            if (node == null)
                return false;
            
            var typeMemberDeclaration = node as ITypeMemberDeclaration;
            return IsPerformanceCriticalRootMethod(typeMemberDeclaration?.DeclaredElement);
        }

        public static bool IsPerformanceCriticalRootMethod([CanBeNull] IDeclaredElement declaredElement)
        {
            var typeMember = declaredElement as ITypeMember;
            var typeElement = typeMember?.GetContainingType();

            if (typeElement == null)
                return false;

            if (typeElement.DerivesFromMonoBehaviour() && declaredElement is IClrDeclaredElement monoBehaviorCLRDeclaredElement)
                return ourKnownHotMonoBehaviourMethods.Contains(monoBehaviorCLRDeclaredElement.ShortName);

            if (typeElement.DerivesFrom(KnownTypes.Editor) && declaredElement is IClrDeclaredElement editorCLRDeclaredElement)
                return ourKnownHotEditorMethods.Contains(editorCLRDeclaredElement.ShortName);
            
            if (typeElement.DerivesFrom(KnownTypes.EditorWindow) && declaredElement is IClrDeclaredElement editorWindowCLRDeclaredElement)
                return ourKnownHotEditorWindowMethods.Contains(editorWindowCLRDeclaredElement.ShortName);
            
            if (typeElement.DerivesFrom(KnownTypes.PropertyDrawer) && declaredElement is IClrDeclaredElement propertyDrawerCLRDeclaredElement)
                return ourKnownHotPropertyDrawerMethods.Contains(propertyDrawerCLRDeclaredElement.ShortName);

            return false;
        }

        #region data

        private static readonly ISet<string> ourKnownHotMonoBehaviourMethods = new HashSet<string>()
        {
            "Update", "LateUpdate", "FixedUpdate", "OnGUI"
        };
        
        private static readonly ISet<string> ourKnownHotEditorMethods = new HashSet<string>()
        {
            "DrawHeader", "OnInspectorGUI", "OnInteractivePreviewGUI", "OnPreviewGUI", "OnSceneGUI"
        };
        
        private static readonly ISet<string> ourKnownHotEditorWindowMethods = new HashSet<string>()
        {
            "OnGUI", "OnInspectorUpdate"
        };
        
        private static readonly ISet<string> ourKnownHotPropertyDrawerMethods = new HashSet<string>()
        {
            "OnGUI", "GetPropertyHeight"
        };

        private static readonly ISet<string> ourKnownComponentCostlyMethods = new HashSet<string>()
        {
            "GetComponentInChildren",
            "GetComponentInParent",
            "GetComponentsInChildren",
            "GetComponent",
            "GetComponents",
            "SendMessage",
            "SendMessageUpwards",
            "BroadcastMessage"
        };

        private static readonly ISet<string> ourKnownGameObjectCostlyMethods = new HashSet<string>()
        {
            "Find",
            "FindGameObjectsWithTag",
            "FindGameObjectWithTag",
            "FindWithTag",
            "GetComponent",
            "GetComponents",
            "GetComponentInChildren",
            "GetComponentInParent",
            "GetComponentsInChildren",
            "SendMessage",
            "SendMessageUpwards",
            "BroadcastMessage"
        };

        private static readonly ISet<string> ourKnownMonoBehaviourCostlyMethods = new HashSet<string>()
        {
            "Invoke",
            "InvokeRepeating",
            "CancelInvoke",
            "IsInvoking"
        };

        private static readonly ISet<string> ourKnownTransformCostlyMethods = new HashSet<string>()
        {
            "Find"
        };

        private static readonly ISet<string> ourKnownResourcesCostlyMethods = new HashSet<string>()
        {
            "FindObjectsOfTypeAll",
        };

        private static readonly ISet<string> ourKnownObjectCostlyMethods = new HashSet<string>()
        {
            "FindObjectsOfType",
            "FindObjectOfType",
            "FindObjectsOfTypeIncludingAssets",
        };

        private static readonly ISet<string> ourKnownDebugCostlyMethods = new HashSet<string>()
        {
            "Log",
            "LogFormat",
            "LogError",
            "LogErrorFormat",
            "LogException",
            "LogWarning",
            "LogWarningFormat",
            "LogAssertion",
            "LogAssertionFormat"
        };
        #endregion
    }
}
