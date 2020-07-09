using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.PerformanceCriticalAnalysis
{
    [ContextAction(
        Group = UnityContextActions.GroupID,
        Name = "PerformanceCriticalAnalysisDisable",
        Description = "Disable Performance critical code analysis",
        Disabled = false,
        AllowedInNonUserFiles = false,
        Priority = 1)]
    public class PerformanceCriticalAnalysisDisableContextAction : PerformanceCriticalAnalysisActionBase
    {
        public PerformanceCriticalAnalysisDisableContextAction(ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }

        protected override IClrTypeName ProtagonistAttribute =>
            CallGraphActionUtil.PerformanceCriticalCodeAnalysisDisableAttribute;

        protected override IClrTypeName AntagonistAttribute =>
            CallGraphActionUtil.PerformanceCriticalCodeAnalysisEnableAttribute;
        public override string Text => "Disable Performance critical code analysis";
    }
}