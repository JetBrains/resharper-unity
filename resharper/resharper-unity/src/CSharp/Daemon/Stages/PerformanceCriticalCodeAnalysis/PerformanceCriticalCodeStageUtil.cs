using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis
{
    internal static class PerformanceCriticalCodeStageUtil
    {
        public static bool IsInvocationExpensive([NotNull] IInvocationExpression invocationExpression)
        {
            invocationExpression.GetPsiServices().Locks.AssertReadAccessAllowed();;

            var reference = (invocationExpression.InvokedExpression as IReferenceExpression)?.Reference;
            if (reference == null)
                return false;

            var declaredElement = reference.Resolve().DeclaredElement as IMethod;

            var containingType = declaredElement?.GetContainingType();
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

            var shortName = declaredElement.ShortName;

            if (knownCostlyMethods != null && knownCostlyMethods.Contains(shortName))
                return true;

            return clrTypeName.Equals(KnownTypes.GameObject) && shortName.Equals("AddComponent") && invocationExpression.TypeArguments.Count == 1;
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

                string baseName = null;
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

        public static IHighlighting CreateHiglighting(ITreeNode treeNode)
        {
            switch (treeNode)
            {
                case IInvocationExpression invocationExpression when IsInvocationExpensive(invocationExpression):
                    return new PerformanceInvocationHighlighting(invocationExpression, (invocationExpression.InvokedExpression as IReferenceExpression).Reference);
                
                case IEqualityExpression equalityExpression when IsNullComparisonWithUnityObject(equalityExpression, out var name):
                    return new PerformanceNullComparisonHighlighting(equalityExpression, name, equalityExpression.Reference.NotNull("eqaulityReference != null"));
                
                case IReferenceExpression referenceExpression when IsCameraMainUsage(referenceExpression):
                    return new PerformanceCameraMainHighlighting(referenceExpression);
            }

            return null;
        }
        

        #region data

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