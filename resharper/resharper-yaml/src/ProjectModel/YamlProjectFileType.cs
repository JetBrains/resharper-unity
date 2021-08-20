using JetBrains.Annotations;
using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Yaml.ProjectModel
{
  [ProjectFileTypeDefinition(Name)]
  public class YamlProjectFileType : KnownProjectFileType
  {
    public new const string Name = "YAML";

    [CanBeNull, UsedImplicitly]
    public new static YamlProjectFileType Instance { get; private set; }

    // This assembly doesn't register any file types. Rider has YAML support on the frontend, and ReSharper doesn't
    // provide any YAML related features. There's no point creating a PSI for files and not doing anything with it.
    // Another plugin is free to register its own file types, like Unity does
    public YamlProjectFileType() : base(Name, "YAML")
    {
    }
  }
}