using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Yaml.Psi.UnityAsset
{
  [ProjectFileTypeDefinition(Name)]
  public class UAProjectFileType : KnownProjectFileType
  {
    public new const string Name = "UA";
    public const string SCENE_EXTENSION = ".unity";
    public const string PREFAB_EXTENSION = ".prefab";
    public const string ASSET_EXTENSION = ".asset";

    public new static readonly UAProjectFileType Instance = null;

    public UAProjectFileType()
      : base(Name, "Unity asset", new[] {SCENE_EXTENSION})
    {
    }
  }
}