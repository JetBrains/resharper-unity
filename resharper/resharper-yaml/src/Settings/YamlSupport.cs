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
      var boundStore = settingsStore.BindToContextLive(lifetime, ContextRange.ApplicationWide);
      IsParsingEnabled = boundStore.GetValueProperty(lifetime, (YamlSettings s) => s.EnableYamlParsing2);
    }
  }
}