#nullable enable
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Util;

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

        public static void SetStringLiteral(this ITreeNode node, ITokenNode? oldLiteral, string text)
        {
            if (oldLiteral == null)
                return;
            Assertion.Assert(oldLiteral.GetTokenType().IsStringLiteral, "Expected string literal for SetStringLiteral");
            var newLiteral = ShaderLabTokenType.STRING_LITERAL.CreateLeafElement($"\"{text}\"");
            using (WriteLockCookie.Create(node.IsPhysical())) 
                ModificationUtil.ReplaceChild(oldLiteral, newLiteral);
        }

        public static void SetStringLiteral(this ITreeNode node, ITokenNode oldLiteral, StringSlice slice, string replacement)
        {
            Assertion.Assert(oldLiteral.GetTokenType().IsStringLiteral, "Expected string literal for SetStringLiteral");
            StringSlice.LowLevelAccess.GetData(ref slice, out var str, out var start, out var length);
            var newLiteral = ShaderLabTokenType.STRING_LITERAL.CreateLeafElement(str.ReplaceRange(start, length, replacement));
            using (WriteLockCookie.Create(node.IsPhysical())) 
                ModificationUtil.ReplaceChild(oldLiteral, newLiteral);
        }

        public static TreeTextRange GetTreeTextRange(this ITreeNode treeNode, in StringSlice slice)
        {
            var tmp = slice;
            StringSlice.LowLevelAccess.GetData(ref tmp, out _, out var start, out var length);
            return TreeTextRange.FromLength(treeNode.GetTreeStartOffset() + start, length);
        }
    }
}