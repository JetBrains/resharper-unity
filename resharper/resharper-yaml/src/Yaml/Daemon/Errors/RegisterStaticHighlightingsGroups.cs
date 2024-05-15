using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Yaml.Resources;

namespace JetBrains.ReSharper.Plugins.Yaml.Daemon.Errors
{
  [RegisterStaticHighlightingsGroup(typeof(Strings), nameof(Strings.YAMLErrors_Text), true)]
  public class YamlErrors
  {
  }
}