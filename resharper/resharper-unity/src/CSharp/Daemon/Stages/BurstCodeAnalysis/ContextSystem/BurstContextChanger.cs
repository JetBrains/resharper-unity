using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.ContextSystem
{
    [SolutionComponent]
    public class BurstContextChanger : IUnityProblemAnalyzerContextChanger
    {
        public UnityProblemAnalyzerContextElement Context => UnityProblemAnalyzerContextElement.BURST_CONTEXT;
        
        public bool IsContextChangingNode(ITreeNode node)
        {
            return BurstCodeAnalysisUtil.IsBurstContextBannedNode(node);
        }
    }
}