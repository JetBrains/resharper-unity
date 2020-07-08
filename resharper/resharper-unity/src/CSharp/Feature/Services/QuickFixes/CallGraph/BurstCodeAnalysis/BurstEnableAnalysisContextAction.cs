using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis
{
    [ContextAction(
        Group = UnityContextActions.GroupID,
        Name = "BurstEnableAnalysis",
        Description = "Enable Burst code analysis",
        Disabled = false,
        AllowedInNonUserFiles = false,
        Priority = 1)]
    public sealed class BurstEnableAnalysisContextAction : BurstAnalysisActionBase
    {
        public BurstEnableAnalysisContextAction(ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }

        protected override IClrTypeName ProtagonistAttribute => CallGraphActionUtil.BurstCodeAnalysisEnableAttribute;
        protected override IClrTypeName AntagonistAttribute => CallGraphActionUtil.BurstCodeAnalysisDisableAttribute;

        public override string Text => "Enable Burst code analysis";
    }
}