using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.FileTypes;
using JetBrains.ReSharper.Plugins.Unity.Utils;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.ProjectModel
{
    [ProjectFileTypeDefinition(Name)]
    public class UnityYamlProjectFileType : BinaryProjectFileType
    {
        public new const string Name = "UnityYaml";

        [CanBeNull, UsedImplicitly]
        public new static UnityYamlProjectFileType Instance { get; private set; }

        public UnityYamlProjectFileType()
            : base(Name, "Unity Yaml", UnityFileExtensions.AllYamlFileExtensionsWithDot)
        {
        }
    }
}