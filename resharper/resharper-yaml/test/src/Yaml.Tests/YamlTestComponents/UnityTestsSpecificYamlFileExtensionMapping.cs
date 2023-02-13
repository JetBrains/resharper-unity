using System;
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Tests.YamlTestComponents
{
  public static class TestYamlProjectFileType
  {
    public const string YAML_EXTENSION = ".yaml";
  }

  // resharper-yaml doesn't register any file types, not even .yaml, because there are no user facing features for yaml
  // for ReSharper, and because Rider has yaml support in the frontend. We also need to register some Unity file types
  // to be sure we can handle Unity specific yaml files.
  [ShellComponent]
  public class UnityTestsSpecificYamlFileExtensionMapping : FileTypeDefinitionExtensionMapping
  {
    private static readonly string[] ourFileExtensions =
    {
      TestYamlProjectFileType.YAML_EXTENSION,

      // This are registered by the Unity plugin, not the YAML plugin. But we need them for tests...
      ".meta",
      ".asset",
      ".unity"
    };

    public UnityTestsSpecificYamlFileExtensionMapping(Lifetime lifetime, IProjectFileTypes fileTypes)
      : base(lifetime, fileTypes)
    {
    }

    public override IEnumerable<ProjectFileType> GetFileTypes(string extension)
    {
      return ourFileExtensions.Contains(extension, StringComparer.InvariantCultureIgnoreCase)
        ? new[] { YamlProjectFileType.Instance! }
        : base.GetFileTypes(extension);
    }

    public override IEnumerable<string> GetExtensions(ProjectFileType projectFileType)
    {
      return Equals(projectFileType, YamlProjectFileType.Instance)
        ? base.GetExtensions(projectFileType).Concat(ourFileExtensions)
        : base.GetExtensions(projectFileType);
    }
  }
}