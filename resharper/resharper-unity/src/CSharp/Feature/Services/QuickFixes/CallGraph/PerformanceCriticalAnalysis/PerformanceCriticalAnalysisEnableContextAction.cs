using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.PerformanceCriticalAnalysis
{
    [ContextAction(
        Group = UnityContextActions.GroupID,
        Name = "PerformanceCriticalAnalysisEnable",
        Description = "Enable Performance critical code analysis",
        Disabled = false,
        AllowedInNonUserFiles = false,
        Priority = 1)]
    public class PerformanceCriticalAnalysisEnableContextAction : PerformanceCriticalAnalysisActionBase
    {
        public PerformanceCriticalAnalysisEnableContextAction(ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }

        protected override IClrTypeName ProtagonistAttribute =>
            CallGraphActionUtil.PerformanceCriticalCodeAnalysisEnableAttribute;

        protected override IClrTypeName AntagonistAttribute =>
            CallGraphActionUtil.PerformanceCriticalCodeAnalysisDisableAttribute;

        public override string Text => "Enable Performance critical code analysis";
    }
}