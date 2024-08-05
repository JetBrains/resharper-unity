using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Settings
{
    [ShellComponent(InstantiationEx.LegacyDefault)]
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