using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem
{
    /// <summary>
    /// Checks if tree node has context.
    /// Each context must have exactly 1 context provider.
    /// </summary>
    public interface IUnityProblemAnalyzerContextProvider : IUnityProblemAnalyzerContextClassification
    {
        UnityProblemAnalyzerContextElement CheckContext(ITreeNode node, DaemonProcessKind processKind);
    }

    public static class UnityProblemAnalyzerContextProviderUtil
    {
        public static bool HasContext(this IUnityProblemAnalyzerContextProvider provider, ITreeNode node, DaemonProcessKind processKind)
        {
            return provider.CheckContext(node, processKind) == provider.Context;
        }
    }
}