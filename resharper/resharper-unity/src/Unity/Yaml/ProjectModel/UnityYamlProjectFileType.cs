using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.FileTypes;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.ProjectModel
{
    [ProjectFileTypeDefinition(Name)]
    public class UnityYamlProjectFileType : BinaryProjectFileType
    {
        public new const string Name = "UnityYaml";

        [UsedImplicitly] public new static UnityYamlProjectFileType? Instance { get; private set; }

        public UnityYamlProjectFileType()
            : base(Name, Strings.UnityYamlProjectFileType_UnityYamlProjectFileType_Unity_Yaml, UnityFileExtensions.YamlDataFileExtensionsWithDot)
        {
        }
    }
}