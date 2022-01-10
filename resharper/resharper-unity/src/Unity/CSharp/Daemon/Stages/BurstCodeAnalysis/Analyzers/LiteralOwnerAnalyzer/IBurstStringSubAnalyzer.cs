using JetBrains.Annotations;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers.LiteralOwnerAnalyzer
{
    public interface IBurstStringSubAnalyzer
    {
        /// <param name="nodeToAnalyze"></param>
        /// <param name="result">false if node not matched, otherwise analyze result</param>
        /// <returns>true if node matched to navigator, false else</returns>
        [ContractAnnotation("nodeToAnalyze:null => false, result:false")]
        bool TryAnalyze([CanBeNull] ICSharpExpression nodeToAnalyze, out bool result);
    }
}