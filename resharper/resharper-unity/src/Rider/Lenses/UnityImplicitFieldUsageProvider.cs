using System.Collections.Generic;
using JetBrains.Application.UI.Controls.GotoByName;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Host.Features.Icons;
using JetBrains.Rider.Model;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Lenses
{
    [SolutionComponent]
    public class UnityImplicitFieldUsageProvider : AbstractUnityImplicitProvider
    {
        public override string ProviderId => "Unity Implicit Field";
        public override string DisplayName => "Set from editor";
        public override ICollection<CodeLensRelativeOrdering> RelativeOrderings => new [] {new CodeLensRelativeOrderingLast()};
        
        public UnityImplicitFieldUsageProvider(IconHost iconHost, BulbMenuComponent bulbMenu)
            : base(bulbMenu)
        {
        }
    }
}