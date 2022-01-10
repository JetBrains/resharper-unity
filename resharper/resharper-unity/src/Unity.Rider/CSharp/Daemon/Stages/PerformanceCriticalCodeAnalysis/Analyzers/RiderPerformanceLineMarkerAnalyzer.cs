using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class RiderPerformanceLineMarkerAnalyzer : PerformanceLineMarkerAnalyzer
    {
        public RiderPerformanceLineMarkerAnalyzer(Lifetime lifetime, IApplicationWideContextBoundSettingStore store)
            : base(lifetime, store)
        {
        }

        protected override void Analyze(IFunctionDeclaration t,
            IHighlightingConsumer consumer, IReadOnlyCallGraphContext context)
        {
            if (LineMarkerStatus.Value == PerformanceHighlightingMode.Always)
            {
                consumer.AddHighlighting(new UnityPerformanceCriticalCodeLineMarker(t.GetDocumentRange()));
            }
        }
    }
}