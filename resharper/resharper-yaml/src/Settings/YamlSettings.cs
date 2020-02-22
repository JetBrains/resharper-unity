using JetBrains.Application.Settings;
using JetBrains.ReSharper.Resources.Settings;

namespace JetBrains.ReSharper.Plugins.Yaml.Settings
{
  [SettingsKey(typeof(CodeEditingSettings), "Yaml plugin settings")]
  public class YamlSettings
  {
    [SettingsEntry(true, "Enables syntax error highlighting, brace matching and more of YAML files.")]
    public bool EnableYamlParsing2;
  }
}