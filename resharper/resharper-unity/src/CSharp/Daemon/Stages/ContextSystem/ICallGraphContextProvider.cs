using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem
{
    public interface ICallGraphContextProvider
    {
        CallGraphContextElement Context { get; }
        bool IsContextAvailable { get; }
        bool IsContextChangingNode(ITreeNode node);
        bool HasContext([CanBeNull] IDeclaration declaration, DaemonProcessKind processKind);
        bool IsCalleeMarked([CanBeNull] ICSharpExpression expression, DaemonProcessKind processKind);
        bool IsMarked([CanBeNull] IDeclaredElement declaredElement, DaemonProcessKind processKind);
    }
    
    public static class CallGraphContextProviderEx
    {
        public static bool IsNodeMarked(
            [CanBeNull] this ICallGraphContextProvider provider, 
            [CanBeNull] ITreeNode treeNode,
            DaemonProcessKind processKind)
        {
            if (provider == null)
                return false;

            var mark = false;
            
            switch (treeNode)
            {
                case ICSharpExpression expression:
                    mark = provider.IsCalleeMarked(expression, processKind);
                    break;
                case IDeclaration declaration:
                    mark = provider.HasContext(declaration, processKind);
                    break;
            }

            return mark;
        }

        public static CallGraphContextElement GetNodeContext(
            [CanBeNull] this ICallGraphContextProvider provider,
            [CanBeNull] ITreeNode treeNode,
            DaemonProcessKind processKind)
        {
            return provider.IsNodeMarked(treeNode, processKind)
                ? provider.Context
                : CallGraphContextElement.NONE;
        }
    }
}