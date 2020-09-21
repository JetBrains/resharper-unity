using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem
{
    /// <summary>
    /// Checks if tree node has context.
    /// Each context must have exactly 1 context provider.
    /// </summary>
    public interface IUnityProblemAnalyzerContextProvider : IUnityProblemAnalyzerContextClassification
    {
        /// <summary>
        /// </summary>
        /// <param name="node"></param>
        /// <param name="processKind"></param>
        /// <param name="getCallee"> is tree node is not declaration - tries to extract it's declared element</param>
        /// <returns></returns>
        UnityProblemAnalyzerContextElement GetContext([CanBeNull] ITreeNode node, DaemonProcessKind processKind, bool getCallee);
        bool IsMarked([CanBeNull] IDeclaredElement node, DaemonProcessKind processKind);
        bool IsProblemContextBound { get; }
    }

    public static class UnityProblemAnalyzerContextProviderEx
    {
        public static bool IsMarked(this IUnityProblemAnalyzerContextProvider provider, [CanBeNull] ITreeNode node,
            DaemonProcessKind processKind, bool getCallee)
        {
            return provider.GetContext(node, processKind, getCallee) == provider.Context;
        }
    }
}