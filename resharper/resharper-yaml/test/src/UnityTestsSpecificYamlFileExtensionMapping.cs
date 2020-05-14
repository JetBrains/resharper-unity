﻿using System;
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Yaml.Tests
{
  [ShellComponent]
  public class UnityTestsSpecificYamlFileExtensionMapping : FileTypeDefinitionExtensionMapping
  {
    private static readonly string[] ourFileExtensions =
    {
#if RIDER
      // Rider doesn't register .yaml, as the frontend already provides support for it. But we need it for tests...
      ".yaml",
#endif

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
      if (ourFileExtensions.Contains(extension, StringComparer.InvariantCultureIgnoreCase))
        return new[] {YamlProjectFileType.Instance};
      return base.GetFileTypes(extension);
    }

    public override IEnumerable<string> GetExtensions(ProjectFileType projectFileType)
    {
      if (Equals(projectFileType, YamlProjectFileType.Instance))
        return base.GetExtensions(projectFileType).Concat(ourFileExtensions);
      return base.GetExtensions(projectFileType);
    }
  }
}