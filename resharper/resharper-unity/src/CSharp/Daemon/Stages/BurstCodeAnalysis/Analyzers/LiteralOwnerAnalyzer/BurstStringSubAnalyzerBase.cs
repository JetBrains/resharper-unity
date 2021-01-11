using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.LiteralOwnerAnalyzer
{
    public abstract class BurstStringSubAnalyzerBase<T> : IBurstStringSubAnalyzer where T : ITreeNode
    {
        /// <summary>
        /// Prefer to use navigators, see examples
        /// </summary>
        /// <param name="expression">navigate from</param>
        /// <returns><typeparamref name="T"/> if can navigate to desired node, null else</returns>
        [CanBeNull]
        [ContractAnnotation("null => null")]
        protected abstract T Navigate([CanBeNull] ICSharpExpression expression);

        protected abstract bool AnalyzeNode([NotNull] T navigated, [NotNull] ICSharpExpression from);

        public bool TryAnalyze(ICSharpExpression nodeToAnalyze, out bool result)
        {
            result = false;

            var navigatedNode = Navigate(nodeToAnalyze);

            if (navigatedNode == null)
                return false;

            result = AnalyzeNode(navigatedNode, nodeToAnalyze);

            return true;
        }
    }
}