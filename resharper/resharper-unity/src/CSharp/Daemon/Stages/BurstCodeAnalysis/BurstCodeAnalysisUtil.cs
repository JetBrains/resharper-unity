using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis
{
    public static class BurstCodeAnalysisUtil
    {
        [ContractAnnotation("null => false")]
        public static bool IsSuitableForBurst([CanBeNull] this IType type)
        {
            if (type == null)
                return false;
            
            return type.IsValueType() || type.IsPointerType() || type.IsOpenType;
        }

        [ContractAnnotation("null => false")]
        public static bool IsAccessedFromOpenType([CanBeNull] this IConditionalAccessExpression conditionalAccessExpression)
        {
            if (conditionalAccessExpression is IInvocationExpression)
            {
                conditionalAccessExpression =
                    conditionalAccessExpression.ConditionalQualifier as IConditionalAccessExpression;
            }
            
            if (conditionalAccessExpression == null)
                return false;

            return conditionalAccessExpression.ConditionalQualifier?.Type().IsOpenType ?? false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="invocation"></param>
        /// <returns></returns>
        public static bool IsBurstProhibitedInvocation([CanBeNull] this IInvocationExpression invocation)
        {
            var function =
                invocation?.InvocationExpressionReference.Resolve().DeclaredElement as IFunction;
            if (function == null)
                return false;
            var containingType = function.GetContainingType();
            //CGTD test for equals + comment that equals prohibited
            if (containingType is IStruct && function is IMethod method && method.IsOverridesObjectGetHashCode())
                return false;
            var isValueTypeOrObject = containingType is IClass @class && (@class.IsSystemValueTypeClass() || @class.IsObjectClass());
            return isValueTypeOrObject || containingType is IStruct && function.IsOverride;
        }

        [ContractAnnotation("null => false")]
        public static bool IsReturnValueBurstProhibited([CanBeNull] this IFunction invokedMethod)
        {
            if (invokedMethod == null)
                return false;
            
            return (invokedMethod.IsStatic || invokedMethod.GetContainingType() is IStruct)
                   && invokedMethod.ReturnType.Classify == TypeClassification.REFERENCE_TYPE;
        }

        [ContractAnnotation("null => false")]
        public static bool HasBurstProhibitedArguments([CanBeNull] this IArgumentList argumentList)
        {
            if (argumentList == null)
                return false;
            
            foreach (var argument in argumentList.Arguments)
            {
                if (!(argument.MatchingParameter?.Type.IsSuitableForBurst() ?? true))
                    return true;
            }

            return false;
        }
    }
}