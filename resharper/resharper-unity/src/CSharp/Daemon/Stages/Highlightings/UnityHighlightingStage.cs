using System.Collections.Generic;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Daemon.CSharp.Stages;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings.IconsProviders;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings
{
    [DaemonStage(StagesBefore = new[] {typeof(CSharpErrorStage)})]
    public class UnityHighlightingStage : UnityHighlightingAbstractStage
    {
        public UnityHighlightingStage(IEnumerable<IUnityDeclarationHighlightingProvider> higlightingProviders,
            UnityApi api, UnityCommonIconProvider commonIconProvider) : base(higlightingProviders, api, commonIconProvider)
        {
        }
    }
}