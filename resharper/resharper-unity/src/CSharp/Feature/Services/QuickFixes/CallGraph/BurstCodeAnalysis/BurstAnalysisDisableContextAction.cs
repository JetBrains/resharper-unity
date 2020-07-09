using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis
{
    [ContextAction(
        Group = UnityContextActions.GroupID,
        Name = "BurstAnalysisDisable",
        Description = "Disable Burst code analysis",
        Disabled = false,
        AllowedInNonUserFiles = false,
        Priority = 1)]
    public sealed class BurstAnalysisDisableContextAction : BurstAnalysisActionBase
    {
        public BurstAnalysisDisableContextAction(ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }

        protected override IClrTypeName ProtagonistAttribute => CallGraphActionUtil.BurstCodeAnalysisDisableAttribute;
        protected override IClrTypeName AntagonistAttribute => CallGraphActionUtil.BurstCodeAnalysisEnableAttribute;

        public override string Text => "Disable Burst code analysis";
    }
}