using System.Collections.Generic;
using JetBrains.Application.UI.Controls.GotoByName;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features.CodeInsights.Providers;
using JetBrains.ReSharper.Host.Features.Icons;
using JetBrains.Rider.Model;


namespace JetBrains.ReSharper.Plugins.Unity.Rider.Lenses
{
    
    [SolutionComponent]
    public class UnityImplicitCodeInsightProvider : AbstractUnityImplicitProvider
    {
        public override string ProviderId => "Unity implicit call";
        public override string DisplayName => "Implicit call";
        public override ICollection<CodeLensRelativeOrdering> RelativeOrderings =>  new[] { new CodeLensRelativeOrderingBefore(ReferencesCodeInsightsProvider.Id)};

        public UnityImplicitCodeInsightProvider(IconHost iconHost, BulbMenuComponent bulbMenu)
            : base(bulbMenu)
        {
        }

    }
}