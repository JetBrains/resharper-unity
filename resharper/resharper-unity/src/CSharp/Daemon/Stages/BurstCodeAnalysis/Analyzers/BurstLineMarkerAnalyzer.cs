using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class BurstLineMarkerAnalyzer : BurstProblemAnalyzerBase<IFunctionDeclaration>
    {
        private readonly IProperty<BurstCodeHighlightingMode> myLineMarkerStatus;

        public BurstLineMarkerAnalyzer(Lifetime lifetime, ISolution solution, ISettingsStore store)
        {
            myLineMarkerStatus = store.BindToContextLive(lifetime, ContextRange.Smart(solution.ToDataContext()))
                .GetValueProperty(lifetime, (UnitySettings key) => key.BurstCodeHighlightingMode);
        }

        protected override bool CheckAndAnalyze(IFunctionDeclaration functionDeclaration,
            IHighlightingConsumer consumer)
        {
            if (consumer == null || myLineMarkerStatus.Value != BurstCodeHighlightingMode.Always)
                return false;
            
            consumer.AddHighlighting(new BurstCodeLineMarker(functionDeclaration.GetDocumentRange()));
            
            return true;
        }
    }
}