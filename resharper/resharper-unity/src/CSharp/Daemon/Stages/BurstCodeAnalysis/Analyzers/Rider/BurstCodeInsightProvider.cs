using System.Collections.Generic;
using JetBrains.Application.UI.Controls.GotoByName;
using JetBrains.Collections.Viewable;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features.CodeInsights.Providers;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights;
using JetBrains.ReSharper.Plugins.Unity.Rider.Protocol;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.Rider
{
    [SolutionComponent]
    public class BurstCodeInsightProvider : AbstractUnityCodeInsightProvider
    {
        
        public BurstCodeInsightProvider(FrontendBackendHost frontendBackendHost, UnitySolutionTracker solutionTracker,
            BulbMenuComponent bulbMenu, UnitySolutionTracker tracker)
            : base(frontendBackendHost, bulbMenu)
        { 
            RelativeOrderings = tracker.IsUnityProject.HasTrueValue()
            // CGTD with performance critical
            ? new CodeLensRelativeOrdering[] {new CodeLensRelativeOrderingBefore(ReferencesCodeInsightsProvider.Id)}
            : new CodeLensRelativeOrdering[] {new CodeLensRelativeOrderingLast()};
        }

        public override string ProviderId => BurstCodeAnalysisUtil.BURST_DISPLAY_NAME;
        public override string DisplayName => BurstCodeAnalysisUtil.BURST_DISPLAY_NAME;
        public override CodeLensAnchorKind DefaultAnchor => CodeLensAnchorKind.Top;
        public override ICollection<CodeLensRelativeOrdering> RelativeOrderings { get; }
    }
}