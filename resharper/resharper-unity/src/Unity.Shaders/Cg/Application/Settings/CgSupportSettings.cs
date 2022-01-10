using JetBrains.Application.Environment;
using JetBrains.Application.Environment.Helpers;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Cg.Application.Settings
{
    [SolutionComponent]
    public class CgSupportSettings
    {
        public IProperty<bool> IsErrorHighlightingEnabled { get; }

        public CgSupportSettings(Lifetime lifetime, IApplicationWideContextBoundSettingStore settingsStore,
                                 RunsProducts.ProductConfigurations productConfigurations)
        {
            IsErrorHighlightingEnabled = settingsStore.BoundSettingsStore
                .GetValueProperty(lifetime, (UnitySettings s) => s.EnableCgErrorHighlighting);

            if (!productConfigurations.IsInternalMode())
            {
                IsErrorHighlightingEnabled.SetValue(lifetime, false);
            }
        }
    }
}