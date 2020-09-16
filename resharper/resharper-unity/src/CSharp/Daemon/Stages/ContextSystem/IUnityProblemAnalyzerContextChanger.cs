using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem
{
    public interface IUnityProblemAnalyzerContextChanger : IUnityProblemAnalyzerContextClassification
    {
        bool IsContextChangingNode(ITreeNode node);
    }
}