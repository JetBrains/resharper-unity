using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph
{
    public static class UnityCallGraphUtil
    {
        /// <summary>
        /// See also <seealso cref="IsContextChangingNode"/>
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static bool IsFunctionNode(ITreeNode node)
        {
            switch (node)
            {
                case IFunctionDeclaration _:
                case ICSharpClosure _:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// This is VERY IMPORTANT function. Most of the call graph problems appears here.
        /// It indicates if context should be changed at this node.
        /// !!At the context changing node EVERY CONTEXT recalculates!!
        /// !!It means that if ANY context needs to be recalculated, OTHERS also will be recalculated!!
        /// There are some common nodes which trigger context changing like <see cref="IsFunctionNode"/>.
        /// But if your context has some special nodes, for example BURST has <see cref="BurstCodeAnalysisUtil.IsBurstContextBannedNode"/>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static bool IsContextChangingNode(ITreeNode node)
        {
            return IsFunctionNode(node) || BurstCodeAnalysisUtil.IsBurstContextBannedNode(node);
        }
    }
}