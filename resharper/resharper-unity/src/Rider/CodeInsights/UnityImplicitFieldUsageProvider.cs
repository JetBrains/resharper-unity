using System.Collections.Generic;
using JetBrains.Application.UI.Controls.GotoByName;
using JetBrains.ProjectModel;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights
{
    [SolutionComponent]
    public class UnityImplicitFieldUsageProvider : AbstractUnityImplicitProvider
    {
        public override string ProviderId => "Unity implicit field";
        public override string DisplayName => "Unity implicit field";
        public override ICollection<CodeLensRelativeOrdering> RelativeOrderings => new [] {new CodeLensRelativeOrderingLast()};
        
        public UnityImplicitFieldUsageProvider(BulbMenuComponent bulbMenu)
            : base(bulbMenu)
        {
        }
    }
}