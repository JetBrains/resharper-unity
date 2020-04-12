using System;
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Yaml.Tests
{
  [ShellComponent]
  public class UnityTestsSpecificYamlFileExtensionMapping : IFileExtensionMapping
  {
    private static readonly string[] ourFileExtensions =
    {
#if RIDER
      // Rider doesn't register .yaml, as the frontend already provides support for it. But we need it for tests...
      ".yaml",
#endif
      ".meta",
      ".asset",
      ".unity"
    };

    public UnityTestsSpecificYamlFileExtensionMapping(Lifetime lifetime)
    {
      Changed = new SimpleSignal(lifetime, GetType().Name + "::Changed");
    }

    public IEnumerable<ProjectFileType> GetFileTypes(string extension)
    {
      if (ourFileExtensions.Contains(extension, StringComparer.InvariantCultureIgnoreCase))
        return new[] {YamlProjectFileType.Instance};
      return EmptyList<ProjectFileType>.Enumerable;
    }

    public IEnumerable<string> GetExtensions(ProjectFileType projectFileType)
    {
      if (Equals(projectFileType, YamlProjectFileType.Instance))
        return ourFileExtensions;
      return EmptyList<string>.Enumerable;
    }

    public ISimpleSignal Changed { get; }
  }
}