using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers
{
    public interface IBurstBannedAnalyzer
    {
        bool Check(ITreeNode node);
    }
}