using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis
{
    [ContextAction(
        Group = UnityContextActions.GroupID,
        Name = Message,
        Description = Message,
        Disabled = false,
        AllowedInNonUserFiles = false,
        Priority = 1)]
    public sealed class BurstCodeAnalysisAddDiscardAttributeContextAction : BurstCodeAnalysisAddDiscardAttributeAction
    {
        public BurstCodeAnalysisAddDiscardAttributeContextAction(ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }
    }

}