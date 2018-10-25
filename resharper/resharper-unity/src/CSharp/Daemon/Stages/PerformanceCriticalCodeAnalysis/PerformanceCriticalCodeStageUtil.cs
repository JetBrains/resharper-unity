using System.Collections.Generic;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis
{
    public static class PerformanceCriticalCodeStageUtil
    {
        public static bool IsInvocationExpensive(IInvocationExpression invocationExpression)
        {
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

            if (clrTypeName.Equals(KnownTypes.GameObject))
                knownCostlyMethods = ourKnownGameObjectCostlyMethods;
                
            if (clrTypeName.Equals(KnownTypes.Resources))
                knownCostlyMethods = ourKnownResourcesCostlyMethods;
                
            if (clrTypeName.Equals(KnownTypes.Object))
                knownCostlyMethods = ourKnownObjectCostlyMethods;
                
            if (clrTypeName.Equals(KnownTypes.Transform))
                knownCostlyMethods = ourKnownTransformCostlyMethods;

            var shortName = declaredElement.ShortName;

            if (knownCostlyMethods != null && knownCostlyMethods.Contains(shortName))
                return true;

            return clrTypeName.Equals(KnownTypes.GameObject) && shortName.Equals("AddComponent") && invocationExpression.TypeArguments.Count == 1;
        }
        
        #region data
            
        private static readonly ISet<string> ourKnownComponentCostlyMethods = new HashSet<string>()
        {
            "GetComponentInChildren",
            "GetComponentInParent",
            "GetComponentsInChildren",
            "GetComponent",
            "GetComponents",
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
            
        #endregion
    }
}