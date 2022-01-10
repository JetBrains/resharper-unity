using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.UnityHighlightingStage
{
    [DaemonStage(GlobalAnalysisStage = true, OverridenStages = new[] {typeof(UnityHighlightingStage)})]
    public class UnityHighlightingGlobalStage : UnityHighlightingAbstractStage
    {
        public UnityHighlightingGlobalStage(
            IEnumerable<IUnityDeclarationHighlightingProvider> highlightingProviders,
            UnityApi api,
            UnityCommonIconProvider commonIconProvider)
            : base(highlightingProviders, api, commonIconProvider)
        {
        }
    }
}