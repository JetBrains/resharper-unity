using JetBrains.Annotations;
using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.ProjectModel
{
    [ProjectFileTypeDefinition(Name)]
    public class ShaderLabProjectFileType : KnownProjectFileType
    {
        public new const string Name = "SHADERLAB";
        public const string SHADERLAB_EXTENSION = ".shader";

        [CanBeNull, UsedImplicitly]
        public new static ShaderLabProjectFileType Instance { get; private set; }

        public ShaderLabProjectFileType()
            : base(Name, "ShaderLab", new[] {SHADERLAB_EXTENSION})
        {
        }
    }
}