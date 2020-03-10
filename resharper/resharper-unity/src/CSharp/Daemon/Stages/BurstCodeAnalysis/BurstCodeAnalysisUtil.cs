using System;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis
{
    public static class BurstCodeAnalysisUtil
    {
        public static bool IsSuitableForBurst(this IType type)
        {
            return type.IsValueType() || type.IsPointerType() || type.IsOpenType;
        }
        
        public static bool IsQualifierOpenType(IConditionalAccessExpression conditionalAccessExpression)
        {
            if (conditionalAccessExpression is IInvocationExpression)
            {
                conditionalAccessExpression = conditionalAccessExpression.ConditionalQualifier as IConditionalAccessExpression;
            }

            return conditionalAccessExpression?.ConditionalQualifier?.Type().IsOpenType ?? false;
        }

        private static bool IsGetHashCode(this IFunction function)
        {
            return function is IMethod && function.IsOverride && function.ShortName == "GetHashCode" && function.Parameters.Count == 0;
        }

        private static bool MayContainProhibitedMethods(this ITypeElement typeElement)
        {
            return typeElement is IClass @class && (@class.IsValueTypeClass() || @class.IsObjectClass());
        }

        public static bool IsBurstProhibitedMethod(this IFunction function)
        {
            if (function == null)
                return false;
            var containingType = function.GetContainingType();
            if (containingType is IStruct && function.IsGetHashCode())
                return false;
            return containingType.MayContainProhibitedMethods() || 
                   containingType is IStruct && function.IsOverride;
        }
        
    }
}