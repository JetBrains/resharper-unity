using System;
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.Settings;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Yaml.Tests
{
#if RIDER
  // Rider doesn't register .yaml, as the frontend already provides support for it. But we need it for tests...
  [ShellComponent]
  public class RiderTestsSpecificYamlFileExtensionMapping : IFileExtensionMapping
  {
    private static readonly string[] ourFileExtensions = {".yaml"};

    public RiderTestsSpecificYamlFileExtensionMapping(Lifetime lifetime)
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

  [ShellComponent]
  public class EnsureEnabledForTests
  {
    public EnsureEnabledForTests(YamlSupport yamlSupport)
    {
      yamlSupport.IsParsingEnabled.SetValue(true);
    }
  }
#endif
}