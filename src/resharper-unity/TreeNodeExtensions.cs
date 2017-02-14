using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity
{
    public static class TreeNodeExtensions
    {
        public static bool IsFromUnityProject(this ITreeNode treeNode)
        {
            return treeNode.GetProject().IsUnityProject();
        }
    }
}