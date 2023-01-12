using System.Collections.Generic;
using JetBrains.Application.UI.Controls.GotoByName;
using JetBrains.Collections.Viewable;
using JetBrains.ProjectModel;
using JetBrains.RdBackend.Common.Features.CodeInsights.Providers;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.Protocol;
using JetBrains.Rider.Model;
using JetBrains.ReSharper.Plugins.Unity.Rider.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.CodeInsights
{
    [SolutionComponent]
    public class UnityCodeInsightProvider : AbstractUnityCodeInsightProvider
    {
        public override string ProviderId => "Unity implicit usage";
        public override string DisplayName => Strings.UnityImplicitUsage_Text;
        public override CodeVisionAnchorKind DefaultAnchor => CodeVisionAnchorKind.Top;

        public override ICollection<CodeVisionRelativeOrdering> RelativeOrderings { get; }

        public UnityCodeInsightProvider(IFrontendBackendHost frontendBackendHost, UnitySolutionTracker solutionTracker,
                                        BulbMenuComponent bulbMenu, UnitySolutionTracker tracker)
            : base(frontendBackendHost, bulbMenu)
        {
            RelativeOrderings = tracker.IsUnityProject.HasTrueValue()
                ? new CodeVisionRelativeOrdering[] {new CodeVisionRelativeOrderingBefore(ReferencesCodeInsightsProvider.Id)}
                : new CodeVisionRelativeOrdering[] {new CodeVisionRelativeOrderingLast()};
        }
    }
}