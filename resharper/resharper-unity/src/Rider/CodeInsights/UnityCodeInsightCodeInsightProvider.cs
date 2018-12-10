using System.Collections.Generic;
using JetBrains.Application.UI.Controls.GotoByName;
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
        
        public override ICollection<CodeLensRelativeOrdering> RelativeOrderings =>  new[] { new CodeLensRelativeOrderingBefore(ReferencesCodeInsightsProvider.Id)};

        public UnityCodeInsightProvider(UnityHost host,BulbMenuComponent bulbMenu)
            : base(host, bulbMenu)
        {
        }

    }
}