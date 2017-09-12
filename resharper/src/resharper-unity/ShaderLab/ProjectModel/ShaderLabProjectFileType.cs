using JetBrains.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.ShaderLab.ProjectModel
{
    [ProjectFileTypeDefinition(Name)]
    public class ShaderLabProjectFileType : KnownProjectFileType
    {
        public new const string Name = "SHADERLAB";
        public const string SHADER_EXTENSION = ".shader";

        public new static readonly ShaderLabProjectFileType Instance = null;

        public ShaderLabProjectFileType()
            : base(Name, "ShaderLab", new[] {SHADER_EXTENSION})
        {
        }
    }
}