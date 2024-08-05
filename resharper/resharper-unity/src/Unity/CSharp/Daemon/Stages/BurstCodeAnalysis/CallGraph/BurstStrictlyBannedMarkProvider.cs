using JetBrains.Annotations;
using JetBrains.Application.Parts;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.CallGraphStage;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.CallGraph
{
    [SolutionComponent(InstantiationEx.LegacyDefault)]
    public class BurstStrictlyBannedMarkProvider : CallGraphCommentMarksProvider
    {
        public const string MarkId = "Unity.BustContextStrictMarks";
        public static readonly CallGraphRootMarkId RootMarkId = new CallGraphRootMarkId(MarkId);
        
        public BurstStrictlyBannedMarkProvider(
            Lifetime lifetime,
            [NotNull] UnitySolutionTracker tracker)
            : base(BurstMarksProvider.MarkId, MarkId, new SimplePropagator())
        {
            Enabled.Value = tracker.IsUnityProject.HasTrueValue();
            tracker.HasUnityReference.Advise(lifetime, b => Enabled.Value = Enabled.Value | b);
        }

        public override LocalList<IDeclaredElement> GetRootMarksFromNode(ITreeNode currentNode, IDeclaredElement containingFunction)
        {
            return new LocalList<IDeclaredElement>();
        }

        public override LocalList<IDeclaredElement> GetBanMarksFromNode(ITreeNode currentNode, IDeclaredElement containingFunction)
        {
            var result = base.GetBanMarksFromNode(currentNode, containingFunction);

            switch (currentNode)
            {
                case ILambdaExpression lambdaExpression:
                {
                    if (BurstCodeAnalysisUtil.IsWithoutBurstForEachLambdaExpression(lambdaExpression))
                        result.Add(lambdaExpression.DeclaredElement);
                    break;
                }
                case IFunctionDeclaration { DeclaredElement: { } function }:
                {
                    if (containingFunction == null)
                        break;
                    if (BurstCodeAnalysisUtil.IsBurstProhibitedFunction(function))
                        result.Add(function);
                    break;
                }
            }

            return result;
        }
    }
}