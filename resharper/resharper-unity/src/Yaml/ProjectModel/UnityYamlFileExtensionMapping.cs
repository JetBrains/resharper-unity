using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.ProjectModel;
using JetBrains.ReSharper.Plugins.Yaml.Psi.UnityAsset;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.ProjectModel
{
    [ShellComponent]
    public class UnityYamlFileExtensionMapping : IFileExtensionMapping
    {
        public UnityYamlFileExtensionMapping(Lifetime lifetime)
        {
            Changed = new SimpleSignal(lifetime, GetType().Name + "::Changed");
        }

        public IEnumerable<ProjectFileType> GetFileTypes(string extension)
        {
            if (UnityYamlFileExtensions.Contains(extension))
                return new[] {UAProjectFileType.Instance};
            return EmptyList<ProjectFileType>.Enumerable;
        }

        public IEnumerable<string> GetExtensions(ProjectFileType projectFileType)
        {
            if (Equals(projectFileType, UAProjectFileType.Instance))
                return UnityYamlFileExtensions.AllFileExtensionsWithDot;
            return EmptyList<string>.Enumerable;
        }

        public ISimpleSignal Changed { get; }
    }
}