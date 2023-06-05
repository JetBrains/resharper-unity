#nullable enable
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Tree
{
    internal static class ShaderLabTreeHelpers
    {
        public static TDst? LastOrDefaultOfType<TSrc, TDst>(this TreeNodeEnumerable<TSrc> stateCommands) where TSrc : class, ITreeNode
        {
            TDst? result = default;
            foreach (var src in stateCommands)
            {
                if (src is TDst dest)
                    result = dest;
            }

            return result;
        }

        public static string FormatCommandDeclaredName(TokenNodeType keywordToken, ITreeNode? nameToken)
        {
            if (nameToken == null || nameToken.GetText() is not {} name || string.IsNullOrEmpty(name))
                return keywordToken.TokenRepresentation;
            return $"{keywordToken.TokenRepresentation} {name}";
        }
    }
}