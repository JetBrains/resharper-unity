using JetBrains.Application.Parts;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph
{
    [SolutionComponent(InstantiationEx.LegacyDefault)]
    public class ExpensiveCodeMarksProvider : PerformanceAnalysisRootMarksProviderBase
    {
        public const string MarkId = "Unity.ExpensiveCode";

        public ExpensiveCodeMarksProvider(Lifetime lifetime, ISolution solution,
            UnitySolutionTracker unitySolutionTracker)
            : base(MarkId, new CallGraphIncomingPropagator(solution, MarkId))
        {
            Enabled.Value = unitySolutionTracker.IsUnityProject.HasTrueValue();
            unitySolutionTracker.HasUnityReference.Advise(lifetime, b => Enabled.Value = Enabled.Value | b);
        }

        public override LocalList<IDeclaredElement> GetRootMarksFromNode(ITreeNode currentNode,
            IDeclaredElement containingFunction)
        {
            var result = base.GetRootMarksFromNode(currentNode, containingFunction);

            // it means we are in functional type member like methodDeclaration
            if (containingFunction == null)
                return result;

            var declaration = currentNode as IDeclaration;
            var declaredElement = declaration?.DeclaredElement;

            if (declaredElement == null || !UnityCallGraphUtil.IsFunctionNode(declaration))
                return result;
            
            using (var processor = new ExpensiveCodeProcessor(declaration))
            {
                declaration.ProcessThisAndDescendants(processor);

                if (processor.ProcessingIsFinished)
                    result.Add(declaredElement);
            }

            return result;
        }

        private sealed class ExpensiveCodeProcessor : UnityCallGraphCodeProcessor
        {
            public ExpensiveCodeProcessor(ITreeNode startTreeNode)
                : base(startTreeNode)
            {
            }

            public override void ProcessBeforeInterior(ITreeNode element)
            {
                switch (element)
                {
                    case IInvocationExpression invocationExpression when
                        PerformanceCriticalCodeStageUtil.IsInvocationExpensive(invocationExpression):
                    case IReferenceExpression referenceExpression when
                        PerformanceCriticalCodeStageUtil.IsCameraMainUsage(referenceExpression):
                    {
                        ProcessingIsFinished = true;
                        break;
                    }
                }
            }
        }
    }
}