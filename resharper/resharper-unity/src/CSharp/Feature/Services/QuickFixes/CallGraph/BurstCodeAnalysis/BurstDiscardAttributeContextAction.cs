using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis
{
    [ContextAction(
        Group = UnityContextActions.GroupID,
        Name = "BurstDisableAnalysis",
        Description = "Disable Burst code analysis",
        Disabled = false,
        AllowedInNonUserFiles = false,
        Priority = 1)]
    public sealed class BurstDiscardAttributeContextAction : BurstDiscardAttributeAction
    {
        public BurstDiscardAttributeContextAction(ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }

        public BurstDiscardAttributeContextAction(IBurstHighlighting burstHighlighting)
            : base(burstHighlighting)
        {
        }
    }

}