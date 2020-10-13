using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Highlightings.Rider;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.Rider
{
    [SolutionComponent]
    public class BurstLineMarkerAnalyzer : BurstProblemAnalyzerBase<IFunctionDeclaration>
    {
        private readonly IProperty<BurstCodeHighlightingMode> myLineMarkerStatus;

        public BurstLineMarkerAnalyzer(Lifetime lifetime, IApplicationWideContextBoundSettingStore store)
        {
            myLineMarkerStatus = store.BoundSettingsStore.GetValueProperty(lifetime, (UnitySettings key) => key.BurstCodeHighlightingMode);
        }

        protected override bool CheckAndAnalyze(IFunctionDeclaration functionDeclaration,
            IHighlightingConsumer consumer)
        {
            if (myLineMarkerStatus.Value == BurstCodeHighlightingMode.Always)
                consumer?.AddHighlighting(new BurstCodeLineMarker(functionDeclaration.GetDocumentRange()));
            
            return false;
        }
    }
}