using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Intentions.CSharp.DisableWarning;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.ExpensiveCodeAnalysis
{
    public abstract class AddExpensiveMethodAttributeActionBase : CallGraphActionBase
    {
        protected const string MESSAGE = "Add Expensive method attribute";
        protected override IClrTypeName ProtagonistAttribute => DisableBySuppressMessageHelper.SuppressMessageAttributeFqn;
        public override string Text => MESSAGE;

        public override bool IsAvailable(IUserDataHolder cache)
        {
            var method = MethodDeclaration?.DeclaredElement;

            return method != null &&
                   MethodDeclaration.IsValid() &&
                   method.IsValid() && 
                   !PerformanceCriticalCodeStageUtil.HasPerformanceSensitiveAttribute(method);
        }
    }
}