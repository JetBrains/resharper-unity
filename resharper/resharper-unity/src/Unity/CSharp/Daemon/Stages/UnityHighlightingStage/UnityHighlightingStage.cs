using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.UnityHighlightingStage
{
    [DaemonStage(Instantiation.DemandAnyThread, StagesBefore = new[] {typeof(CSharpErrorStage), typeof(SmartResolverStage)})]
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