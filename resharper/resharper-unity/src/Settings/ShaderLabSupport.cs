using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;

namespace JetBrains.ReSharper.Plugins.Unity.Settings
{
    [ShellComponent]
    public class ShaderLabSupport
    {
        public IProperty<bool> IsParsingEnabled { get; }

        public ShaderLabSupport(Lifetime lifetime, ISettingsStore settingsStore)
        {
            // We can't use IApplicationWideContextBoundSettingsStore here because this a ShellComponent, because it's used
            // in ShaderLabProjectFileLanguageService
            // Keep a live context so that we'll get new mount points, e.g. Solution
            IsParsingEnabled = settingsStore.BindToContextLive(lifetime, ContextRange.ApplicationWide)
                .GetValueProperty(lifetime, (UnitySettings s) => s.EnableShaderLabParsing);
        }
    }
}