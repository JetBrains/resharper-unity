using JetBrains.Application;
using JetBrains.Application.Environment;
using JetBrains.Application.Environment.Helpers;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;

namespace JetBrains.ReSharper.Plugins.Unity.Settings
{
    [ShellComponent]
    public class CgSupportSettings
    {
        public IProperty<bool> IsErrorHighlightingEnabled { get; }

        public CgSupportSettings(Lifetime lifetime, ISettingsStore settingsStore, RunsProducts.ProductConfigurations productConfigurations)
        {
            var boundStore = settingsStore.BindToContextLive(lifetime, ContextRange.ApplicationWide);
            IsErrorHighlightingEnabled = boundStore.GetValueProperty(lifetime, (UnitySettings s) => s.EnableCgErrorHighlighting);

            if (!productConfigurations.IsInternalMode())
            {
                IsErrorHighlightingEnabled.SetValue(lifetime, false);
            }
        }
    }
}