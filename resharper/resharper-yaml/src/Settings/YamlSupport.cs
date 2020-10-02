using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;

namespace JetBrains.ReSharper.Plugins.Yaml.Settings
{
  [ShellComponent]
  public class YamlSupport
  {
    public IProperty<bool> IsParsingEnabled { get; }

    public YamlSupport(Lifetime lifetime, ISettingsStore settingsStore)
    {
      // We can't use IApplicationWideContextBoundSettingsStore here because this a ShellComponent, because it's used
      // in UnityYamlProjectFileLanguageService
      // Keep a live context so that we'll get new mount points, e.g. Solution
      IsParsingEnabled = settingsStore.BindToContextLive(lifetime, ContextRange.ApplicationWide)
        .GetValueProperty(lifetime, (YamlSettings s) => s.EnableYamlParsing2);
    }
  }
}