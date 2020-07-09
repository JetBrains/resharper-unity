using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.PerformanceCriticalCodeAnalysis
{
    [ContextAction(
        Group = UnityContextActions.GroupID,
        Name = MESSAGE,
        Description = MESSAGE,
        Disabled = false,
        AllowedInNonUserFiles = false,
        Priority = 1)]
    public class PerformanceCriticalCodeAnalysisDisableContextAction : PerformanceCriticalCodeAnalysisActionBase
    {
        private const string MESSAGE = "Disable Performance critical code analysis";
        public PerformanceCriticalCodeAnalysisDisableContextAction(ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }

        protected override IClrTypeName ProtagonistAttribute =>
            CallGraphActionUtil.PerformanceCriticalCodeAnalysisDisableAttribute;

        protected override IClrTypeName AntagonistAttribute =>
            CallGraphActionUtil.PerformanceCriticalCodeAnalysisEnableAttribute;
        public override string Text => MESSAGE;
    }
}