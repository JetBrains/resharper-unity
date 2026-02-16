using JetBrains.Application.Components;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.UnityHighlightingStage
{
    [DaemonStage(Instantiation.DemandAnyThreadSafe,
        GlobalAnalysisStage = true,
        StagesBefore = [typeof(SolutionAnalysisFileStructureCollectorStage)],
        OverridenStages = [typeof(UnityHighlightingStage)])]
    public class UnityHighlightingGlobalStage : UnityHighlightingAbstractStage
    {
        public UnityHighlightingGlobalStage(
            IImmutableEnumerable<IUnityDeclarationHighlightingProvider> highlightingProviders,
            UnityApi api,
            UnityCommonIconProvider commonIconProvider)
            : base(highlightingProviders, api, commonIconProvider)
        {
        }
    }
}