using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.Application.Settings;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Settings
{
    [ShellComponent(Instantiation.DemandAnyThreadSafe)]
    public class ShaderLabSupport
    {
        private readonly IContextBoundSettingsStoreLive mySettingsStore;
        private readonly SettingsScalarEntry myIsParsingEnabledEntry;

        public ShaderLabSupport(Lifetime lifetime, ISettingsStore settingsStore)
        {
            // We can't use IApplicationWideContextBoundSettingsStore here because this a ShellComponent, because it's used
            // in ShaderLabProjectFileLanguageService
            // Keep a live context so that we'll get new mount points, e.g. Solution
            mySettingsStore = settingsStore.BindToContextLive(lifetime, ContextRange.ApplicationWide);
            myIsParsingEnabledEntry = mySettingsStore.Schema.GetScalarEntry(static (UnitySettings s) => s.EnableShaderLabParsing);
        }

        public bool IsParsingEnabled => mySettingsStore.GetValue(myIsParsingEnabledEntry, null) is true;
    }
}