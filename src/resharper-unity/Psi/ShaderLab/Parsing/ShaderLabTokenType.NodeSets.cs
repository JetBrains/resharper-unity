using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Psi.ShaderLab.Parsing
{
    public partial class ShaderLabTokenType
    {
        public static readonly NodeTypeSet KEYWORDS;

        static ShaderLabTokenType()
        {
            KEYWORDS = new NodeTypeSet(
                SHADER_KEYWORD,
                PROPERTIES_KEYWORD,
                SUB_SHADER_KEYWORD,
                FALLBACK_KEYWORD,
                CUSTOM_EDITOR_KEYWORD,
                DEPENDENCY_KEYWORD,

                COLOR_KEYWORD,
                CUBE_KEYWORD,
                FLOAT_KEYWORD,
                INT_KEYWORD,
                RANGE_KEYWORD,
                TEXTURE_2D_KEYWORD,
                TEXTURE_3D_KEYWORD,
                VECTOR_KEYWORD,

                TAGS_KEYWORD,
                PASS_KEYWORD,
                USEPASS_KEYWORD,
                GRABPASS_KEYWORD,

                NAME_KEYWORD,

                OFF_KEYWORD
              );
        }
    }
}