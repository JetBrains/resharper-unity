using System.Collections.Generic;
using JetBrains.Application.UI.Controls.GotoByName;
using JetBrains.Collections.Viewable;
using JetBrains.ProjectModel;
using JetBrains.RdBackend.Common.Features.CodeInsights.Providers;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.CodeInsights;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.Protocol;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    [SolutionComponent]
    public class BurstCodeInsightProvider : AbstractUnityCodeInsightProvider
    {
        public BurstCodeInsightProvider(IFrontendBackendHost frontendBackendHost,
                                        BulbMenuComponent bulbMenu,
                                        UnitySolutionTracker tracker)
            : base(frontendBackendHost, bulbMenu)
        {
            RelativeOrderings = tracker.IsUnityProject.HasTrueValue()
                ? new CodeLensRelativeOrdering[] {new CodeLensRelativeOrderingBefore(ReferencesCodeInsightsProvider.Id)}
                : new CodeLensRelativeOrdering[] {new CodeLensRelativeOrderingLast()};
        }

        public override string ProviderId => BurstCodeAnalysisUtil.BURST_DISPLAY_NAME;
        public override string DisplayName => BurstCodeAnalysisUtil.BURST_DISPLAY_NAME;
        public override CodeLensAnchorKind DefaultAnchor => CodeLensAnchorKind.Top;
        public override ICollection<CodeLensRelativeOrdering> RelativeOrderings { get; }
    }
}
