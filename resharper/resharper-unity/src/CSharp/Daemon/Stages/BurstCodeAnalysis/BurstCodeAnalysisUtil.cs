using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
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
            return expression.Parent?.Parent is IAttribute attribute && attribute.Name.ShortName == "DllImport";
        }
    }
}