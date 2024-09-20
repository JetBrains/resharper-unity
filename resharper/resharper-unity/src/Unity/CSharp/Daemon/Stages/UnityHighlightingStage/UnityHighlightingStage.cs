using System.Collections.Generic;
using JetBrains.Application.Components;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration.Api;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.UnityHighlightingStage
{
    [DaemonStage(Instantiation.DemandAnyThreadUnsafe, StagesBefore = new[] {typeof(CSharpErrorStage), typeof(SmartResolverStage)})]
    public class UnityHighlightingStage : UnityHighlightingAbstractStage
    {
        public UnityHighlightingStage(
            IImmutableEnumerable<IUnityDeclarationHighlightingProvider> highlightingProviders,
            UnityApi api, 
            UnityCommonIconProvider commonIconProvider)
            : base(highlightingProviders, api, commonIconProvider)
        {
        }
    }
}