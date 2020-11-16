using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CallGraph;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph
{
    [SolutionComponent]
    public class ExpensiveCodeMarksProvider : CallGraphRootMarksProviderBase
    {
        public const string MarkId = "Unity.ExpensiveCode";
        public readonly CallGraphRootMarksProviderId ProviderId = new CallGraphRootMarksProviderId(MarkId);

        public ExpensiveCodeMarksProvider(Lifetime lifetime, ISolution solution,
            UnityReferencesTracker referencesTracker,
            UnitySolutionTracker unitySolutionTracker)
            : base(MarkId, new CallGraphIncomingPropagator(solution, MarkId))
        {
            Enabled.Value = unitySolutionTracker.IsUnityProject.HasTrueValue();
            referencesTracker.HasUnityReference.Advise(lifetime, b => Enabled.Value = Enabled.Value | b);
        }

        public override LocalList<IDeclaredElement> GetRootMarksFromNode(ITreeNode currentNode,
            IDeclaredElement containingFunction)
        {
            var result = new LocalList<IDeclaredElement>();

            // it means we are in functional type member like methodDeclaration
            if (containingFunction == null)
                return result;

            var declaration = currentNode as IDeclaration;
            var declaredElement = declaration?.DeclaredElement;

            if (declaredElement == null || !UnityCallGraphUtil.IsFunctionNode(declaration))
                return result;

            var hasComment = false;
            
            if(declaration is IFunctionDeclaration functionDeclaration)
                hasComment = UnityCallGraphUtil.HasAnalysisComment(functionDeclaration, MarkId, ReSharperControlConstruct.Kind.Restore);

            if (hasComment)
                result.Add(declaredElement);
            else
            {
                using (var processor = new ExpensiveCodeProcessor(declaration))
                {
                    declaration.ProcessThisAndDescendants(processor);

                    if (processor.ProcessingIsFinished)
                        result.Add(declaredElement);
                }
            }

            return result;
        }

        public override LocalList<IDeclaredElement> GetBanMarksFromNode(ITreeNode currentNode,
            IDeclaredElement containingFunction)
        {
            var result = new LocalList<IDeclaredElement>();

            // it means we are in functional type member like methodDeclaration
            if (containingFunction == null)
                return result;

            var functionDeclaration = currentNode as IFunctionDeclaration;
            var element = UnityCallGraphUtil.HasAnalysisComment(functionDeclaration, UnityCallGraphUtil.PerformanceExpensiveComment, ReSharperControlConstruct.Kind.Disable);

            if (element)
                result.Add(functionDeclaration.DeclaredElement);

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