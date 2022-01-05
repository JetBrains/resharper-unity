using JetBrains.Annotations;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Text;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public static class TreeNodeExtensions
    {
        public static bool IsFromUnityProject([NotNull] this ITreeNode treeNode)
        {
            return treeNode.GetProject().IsUnityProject();
        }

        public static bool CompareBufferText([NotNull] this ITreeNode node, string value)
        {
            return node.GetTextAsBuffer().CompareBufferText(new TextRange(0, node.GetTextLength()), value);
        }
    }
}