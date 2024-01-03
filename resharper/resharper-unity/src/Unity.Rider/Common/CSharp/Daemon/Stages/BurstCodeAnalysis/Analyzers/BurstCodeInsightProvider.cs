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
                ? new CodeVisionRelativeOrdering[] {new CodeVisionRelativeOrderingBefore(ReferencesCodeInsightsProvider.Id)}
                : new CodeVisionRelativeOrdering[] {new CodeVisionRelativeOrderingLast()};
        }

        public override string ProviderId => "Burst compiled code";
        public override string DisplayName => BurstCodeAnalysisUtil.BurstDisplayName;
        public override CodeVisionAnchorKind DefaultAnchor => CodeVisionAnchorKind.Top;
        public override ICollection<CodeVisionRelativeOrdering> RelativeOrderings { get; }
    }
}
