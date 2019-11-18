using JetBrains.Annotations;
using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Yaml.ProjectModel
{
  [ProjectFileTypeDefinition(Name)]
  public class YamlProjectFileType : KnownProjectFileType
  {
    public new const string Name = "YAML";
    public const string YAML_EXTENSION = ".yaml";

    [CanBeNull, UsedImplicitly]
    public new static YamlProjectFileType Instance { get; private set; }

    public YamlProjectFileType()
#if RIDER
      // Rider has YAML support on the front end. We don't register by default for any file types. If another plugin
      // (e.g. Unity) wishes to have files treated as YAML, then it can register its own file types
      : base(Name, "YAML")
#else
      : base(Name, "YAML", new[] {YAML_EXTENSION})
#endif
    {
    }
  }
}