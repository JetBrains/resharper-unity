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
                CUSTOM_EDITOR_KEYWORD);
        }
    }
}