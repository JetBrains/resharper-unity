using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.ProjectModel
{
    [ProjectFileTypeDefinition(Name)]
    public class UnityYamlProjectFileType : KnownProjectFileType
    {
        public new const string Name = "UnityYaml";

        [CanBeNull, UsedImplicitly]
        public new static UnityYamlProjectFileType Instance { get; private set; }

        public UnityYamlProjectFileType()
            : base(Name, "Unity Yaml", UnityYamlFileExtensions.AllFileExtensionsWithDot)
        {
        }
    }
}