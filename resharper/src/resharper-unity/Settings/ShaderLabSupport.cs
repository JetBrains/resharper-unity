using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;

namespace JetBrains.ReSharper.Plugins.Unity.Settings
{
    [ShellComponent]
    public class ShaderLabSupport
    {
        public IProperty<bool> IsParsingEnabled { get; }

        public ShaderLabSupport(Lifetime lifetime, ISettingsStore settingsStore)
        {
            var boundStore = settingsStore.BindToContextLive(lifetime, ContextRange.ApplicationWide);
            IsParsingEnabled = boundStore.GetValueProperty(lifetime, (UnitySettings s) => s.EnableShaderLabParsing);
        }
    }
}