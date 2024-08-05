using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Utils;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Psi.Modules
{
    [SolutionComponent(InstantiationEx.LegacyDefault)]
    public class AssetIndexingSupport
    {
        public readonly IProperty<bool> IsEnabled;

        public AssetIndexingSupport(Lifetime lifetime,
                                    SolutionWideWritableContextBoundSettingsStore settingsStore)
        {
            // If this property is written to, the changes are saved to solution level settings, specifically .sln.DotSettings.user
            IsEnabled = settingsStore.BoundSettingsStore.GetValueProperty(lifetime,
                (UnitySettings key) => key.IsAssetIndexingEnabled);
        }
    }
}