using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.ProjectModel
{
    [ProjectFileTypeDefinition(Name)]
    public class UnityYamlProjectFileType : KnownProjectFileType
    {
        public new const string Name = "UnityYaml";

        public new static readonly UnityYamlProjectFileType Instance = null;

        public UnityYamlProjectFileType()
            : base(Name, "Unity Yaml", UnityYamlFileExtensions.AllFileExtensionsWithDot)
        {
        }
    }
}