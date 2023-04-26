#nullable enable

using System;
using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi.Parsing;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.ShaderLab.Psi
{
    internal static class TreeNodeHelpers
    {
        public static ITreeNode? FindParent(this ITreeNode node, [RequireStaticDelegate] Func<ITreeNode, bool> predicate)
        {
            for (var parent = node.Parent; parent != null; parent = parent.Parent)
            {
                if (predicate(parent))
                    return parent;
            }

            return null;
        }

        public static T? FindPrevSibling<T>(this ITreeNode treeNode) where T : ITreeNode => (T?)treeNode.FindPrevSibling(static node => node is T);

        public static ITreeNode? FindPrevSibling(this ITreeNode treeNode, [RequireStaticDelegate] Func<ITreeNode, bool> predicate)
        {
            for (var prevSibling = treeNode.PrevSibling; prevSibling != null; prevSibling = prevSibling.PrevSibling)
            {
                if (predicate(prevSibling))
                    return prevSibling;
            }

            return null;
        }

        public static bool HasPrevSiblingOfType(this ITreeNode treeNode, NodeType nodeType)
        {
            for (var prevSibling = treeNode.PrevSibling; prevSibling != null; prevSibling = prevSibling.PrevSibling)
            {
                if (prevSibling.NodeType == nodeType)
                    return true;
            }

            return false;
        }
        
        public static bool HasNextSiblingOfType(this ITreeNode treeNode, NodeType nodeType)
        {
            for (var nextSibling = treeNode.NextSibling; nextSibling != null; nextSibling = nextSibling.NextSibling)
            {
                if (nextSibling.NodeType == nodeType)
                    return true;
            }

            return false;
        }

        public static bool IsNodeWithinBraces(this ITreeNode treeNode) => HasPrevSiblingOfType(treeNode, ShaderLabTokenType.LBRACE) && HasNextSiblingOfType(treeNode, ShaderLabTokenType.RBRACE);
    }
}