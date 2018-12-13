using System.Collections.Generic;
using JetBrains.Application.UI.Controls.GotoByName;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features.CodeInsights.Providers;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights
{
    [SolutionComponent]
    public class UnityCodeInsightProvider : AbstractUnityCodeInsightProvider
    {
        public override string ProviderId => "Unity implicit usage";
        public override string DisplayName => "Unity implicit usage";
        public override CodeLensAnchorKind DefaultAnchor => CodeLensAnchorKind.Top;

        public override ICollection<CodeLensRelativeOrdering> RelativeOrderings { get; }

        public UnityCodeInsightProvider(UnityHost host, BulbMenuComponent bulbMenu, UnitySolutionTracker tracker)
            : base(host, bulbMenu)
        {
            RelativeOrderings = tracker.IsUnityProject.HasValue() && tracker.IsUnityProject.Value
                ? new CodeLensRelativeOrdering[] {new CodeLensRelativeOrderingBefore(ReferencesCodeInsightsProvider.Id)}
                : new CodeLensRelativeOrdering[] {new CodeLensRelativeOrderingLast()};
        }
    }
}