using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Settings
{
    [ShellComponent]
    public class ShaderLabSupport
    {
        public IProperty<bool> IsParsingEnabled { get; }

        public ShaderLabSupport(Lifetime lifetime, IApplicationWideContextBoundSettingStore settingsStore)
        {
            IsParsingEnabled = settingsStore.BoundSettingsStore.
                GetValueProperty(lifetime, (UnitySettings s) => s.EnableShaderLabParsing);
        }
    }
}