using System;
using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.ProjectModel;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.ProjectModel
{
    [ShellComponent]
    public class UnityYamlFileExtensionMapping : IFileExtensionMapping
    {
        // TODO: What else?
        private static readonly string[] ourFileExtensions = {".meta", ".unity", ".asset"};

        public UnityYamlFileExtensionMapping(Lifetime lifetime)
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