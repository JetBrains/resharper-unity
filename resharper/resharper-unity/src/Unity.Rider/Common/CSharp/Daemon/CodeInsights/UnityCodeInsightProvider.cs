using System.Collections.Generic;
using JetBrains.Application.UI.Controls.GotoByName;
using JetBrains.Collections.Viewable;
using JetBrains.ProjectModel;
using JetBrains.RdBackend.Common.Features.CodeInsights.Providers;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.Common.Protocol;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.CodeInsights
{
    [SolutionComponent]
    public class UnityCodeInsightProvider : AbstractUnityCodeInsightProvider
    {
        public override string ProviderId => "Unity implicit usage";
        public override string DisplayName => "Unity implicit usage";
        public override CodeLensAnchorKind DefaultAnchor => CodeLensAnchorKind.Top;

        public override ICollection<CodeLensRelativeOrdering> RelativeOrderings { get; }

        public UnityCodeInsightProvider(IFrontendBackendHost frontendBackendHost, UnitySolutionTracker solutionTracker,
                                        BulbMenuComponent bulbMenu, UnitySolutionTracker tracker)
            : base(frontendBackendHost, bulbMenu)
        {
            RelativeOrderings = tracker.IsUnityProject.HasTrueValue()
                ? new CodeLensRelativeOrdering[] {new CodeLensRelativeOrderingBefore(ReferencesCodeInsightsProvider.Id)}
                : new CodeLensRelativeOrdering[] {new CodeLensRelativeOrderingLast()};
        }
    }
}