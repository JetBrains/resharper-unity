using JetBrains.Application.Settings;
using JetBrains.ReSharper.Resources.Settings;

namespace JetBrains.ReSharper.Plugins.Yaml.Settings
{
  [SettingsKey(typeof(CodeEditingSettings), "Yaml plugin settings")]
  public class YamlSettings
  {
    // Previous setting could be disable due to large assets in solutions. Right now, yaml language is not using
    // for parsing scenes and prefabs (large assets) and used only for parsing meta files & project settings (small assets)
    [SettingsEntry(true, "Enables syntax error highlighting, brace matching and more of YAML files.")]
    public bool EnableYamlParsing2;
  }
}