using JetBrains.Annotations;
using JetBrains.Collections;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.ExpensiveCodeAnalysis
{
    public static class ExpensiveCodeActionsUtil
    {
        public static CompactList<AttributeValue> GetExpensiveAttributeValues([CanBeNull] IMethodDeclaration methodDeclaration)
        {
            if(methodDeclaration == null)
                return new CompactList<AttributeValue>();
            
            var psiModule = methodDeclaration.GetPsiModule();
            var predefinedType = psiModule.GetPredefinedType();
            var fixedArguments = new CompactList<AttributeValue>
            {
                new AttributeValue(new ConstantValue(PerformanceCriticalCodeStageUtil.RESHARPER, predefinedType.String)),
                new AttributeValue(new ConstantValue(PerformanceCriticalCodeStageUtil.CHEAP_METHOD, predefinedType.String))
            };

            return fixedArguments;
        }
    }
}