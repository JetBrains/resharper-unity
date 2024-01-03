using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.UnityEditorIntegration;
using JetBrains.ReSharper.Plugins.Yaml.ProjectModel;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.ProjectModel
{
    [ProjectFileTypeDefinition(Name)]
    public class MetaProjectFileType : YamlProjectFileType
    {
        public new const string Name = "Meta";
        
        [UsedImplicitly] public new static MetaProjectFileType? Instance { get; private set; }

        public MetaProjectFileType()
            : base(Name, Strings.MetaProjectFileType_MetaProjectFileType_Unity_Meta_File, new[] { UnityFileExtensions.MetaFileExtensionWithDot })
        {
        }
    }
}
