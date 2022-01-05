using System.Collections.Generic;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.UnityHighlightingStage
{
    [DaemonStage(StagesBefore = new[] {typeof(CSharpErrorStage)})]
    public class UnityHighlightingStage : UnityHighlightingAbstractStage
    {
        public UnityHighlightingStage(
            IEnumerable<IUnityDeclarationHighlightingProvider> highlightingProviders,
            UnityApi api, 
            UnityCommonIconProvider commonIconProvider)
            : base(highlightingProviders, api, commonIconProvider)
        {
        }
    }
}