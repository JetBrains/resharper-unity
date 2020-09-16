using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem
{
    [SolutionComponent]
    public class EmptyUnityProblemAnalyzerContextProvider : IUnityProblemAnalyzerContextProvider
    {
        public UnityProblemAnalyzerContextElement Context => UnityProblemAnalyzerContextElement.NONE;
        public UnityProblemAnalyzerContextElement GetContext(ITreeNode node, DaemonProcessKind processKind) => Context;
        public bool IsMarked(IDeclaredElement node, DaemonProcessKind processKind) => false;
        public bool IsEnabled => false;
    }
}