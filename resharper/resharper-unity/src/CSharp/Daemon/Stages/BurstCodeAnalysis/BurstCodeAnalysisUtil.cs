using JetBrains.Annotations;
using JetBrains.ReSharper.Features.Navigation.Features.GoToDeclarationUsages.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
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
                conditionalAccessExpression =
                    conditionalAccessExpression.ConditionalQualifier as IConditionalAccessExpression;
            }

            return conditionalAccessExpression?.ConditionalQualifier?.Type().IsOpenType ?? false;
        }
    
        private static bool MayContainProhibitedMethods(this ITypeElement typeElement)
        {
            //                                             CGTD is valueType is what i want? what is this anyway?
            return typeElement is IClass @class && (@class.IsSystemValueTypeClass() || @class.IsObjectClass());
        }

        [ContractAnnotation("null => false")]
        public static bool IsBurstProhibitedMethod(this IFunction function)
        {
            if (function == null)
                return false;
            var containingType = function.GetContainingType();
            if (containingType is IStruct && function is IMethod method && method.IsOverridesObjectGetHashCode())
                return false;
            return containingType.MayContainProhibitedMethods() ||
                   containingType is IStruct && function.IsOverride;
        }

        [ContractAnnotation("null => false")]
        public static bool IsReturnValueProhibited(this IFunction invokedMethod)
        {
            return ((invokedMethod?.IsStatic ?? false) || invokedMethod?.GetContainingType() is IStruct)
                   && invokedMethod.ReturnType.Classify == TypeClassification.REFERENCE_TYPE;
        }

        public static bool HasProhibitedArguments([NotNull] this IArgumentList argumentList)
        {
            foreach (var argument in argumentList.Arguments)
            {
                if (!(argument.MatchingParameter?.Type.IsSuitableForBurst() ?? true))
                    return true;
            }

            return false;
        }
    }
}