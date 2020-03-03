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
        
        public static bool InBurstAttribute(IReferenceExpression expression)
        {
            return expression.GetContainingNode<IAttribute>() is IAttribute attribute && attribute.Name.ShortName == "DllImport";
        }

        public static bool IsGetHashCode(this IFunction function)
        {
            return function is IMethod && function.ShortName == "GetHashCode" && function.Parameters.Count == 0 &&
                   function.ReturnType.IsInt();
        }
        
    }
}