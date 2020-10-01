using JetBrains.Application;
using JetBrains.Application.Settings;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Psi.Util;

namespace JetBrains.ReSharper.Plugins.Yaml.Settings
{
  [ShellComponent]
  public class YamlSupport
  {
    public IProperty<bool> IsParsingEnabled { get; }

    public YamlSupport(Lifetime lifetime, IApplicationWideContextBoundSettingStore settingsStore)
    {
      IsParsingEnabled = settingsStore.BoundSettingsStore
        .GetValueProperty(lifetime, (YamlSettings s) => s.EnableYamlParsing2);
    }
  }
}