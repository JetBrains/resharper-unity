using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.CallGraph
{
    [SolutionComponent]
    public class BurstStrictlyBannedMarkProvider : CallGraphCommentMarksProvider
    {
        public const string MarkId = "Unity.BustContextStrictMarks";
        public static readonly CallGraphRootMarkId RootMarkId = new CallGraphRootMarkId(MarkId);
        
        public BurstStrictlyBannedMarkProvider()
            : base(BurstMarksProvider.MarkId, MarkId, new SimplePropagator(), true)
        {
        }

        public override LocalList<IDeclaredElement> GetRootMarksFromNode(ITreeNode currentNode, IDeclaredElement containingFunction)
        {
            return new LocalList<IDeclaredElement>();
        }

        public override LocalList<IDeclaredElement> GetBanMarksFromNode(ITreeNode currentNode, IDeclaredElement containingFunction)
        {
            var result = base.GetBanMarksFromNode(currentNode, containingFunction);

            if (containingFunction == null)
                return result;

            var functionDeclaration = currentNode as IFunctionDeclaration;
            var function = functionDeclaration?.DeclaredElement;

            if (function == null)
                return result;

            if (BurstCodeAnalysisUtil.IsBurstProhibitedFunction(function))
                result.Add(function);

            return result;
        }
    }
}