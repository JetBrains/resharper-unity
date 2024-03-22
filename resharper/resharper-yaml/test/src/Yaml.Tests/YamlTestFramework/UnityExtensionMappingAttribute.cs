using System;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.ProjectModel;
using JetBrains.ReSharper.TestFramework;

namespace JetBrains.ReSharper.Plugins.Tests.YamlTestFramework
{
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
  internal class UnityExtensionMappingAttribute : CustomExtensionMappingAttribute
  {
    private static readonly string[] ourFileExtensions =
    {
      TestYamlProjectFileType.YAML_EXTENSION,

      // These are registered by the Unity plugin, not the YAML plugin. But we need them for tests...
      ".meta",
      ".asset",
      ".unity"
    };
    
    public UnityExtensionMappingAttribute() : base(typeof(YamlProjectFileType), ourFileExtensions) { }

    protected override Lazy<ProjectFileType?> ProjectFileTypeInstance { get; } = new(() => YamlProjectFileType.Instance);
  }
}