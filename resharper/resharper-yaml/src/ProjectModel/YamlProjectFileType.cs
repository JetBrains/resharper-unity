using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Yaml.ProjectModel
{
  [ProjectFileTypeDefinition(Name)]
  public class YamlProjectFileType : KnownProjectFileType
  {
    public new const string Name = "YAML";
    public const string YAML_EXTENSION = ".yaml";

    public new static readonly YamlProjectFileType Instance = null;

    public YamlProjectFileType()
      : base(Name, "YAML", new[] {YAML_EXTENSION})
    {
    }
  }
}