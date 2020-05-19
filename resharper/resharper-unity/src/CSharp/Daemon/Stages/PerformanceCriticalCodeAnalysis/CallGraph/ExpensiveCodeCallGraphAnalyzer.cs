using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.CallGraph
{
    [SolutionComponent]
    public class ExpensiveCodeCallGraphAnalyzer : CallGraphRootMarksProviderBase
    {
        public const string MarkId = "Unity.ExpensiveCode";

        public ExpensiveCodeCallGraphAnalyzer(Lifetime lifetime, ISolution solution, UnityReferencesTracker referencesTracker,
            UnitySolutionTracker unitySolutionTracker)
            : base(MarkId, new CallGraphIncomingPropagator(solution, MarkId))
        {
            Enabled.Value = unitySolutionTracker.IsUnityProject.HasTrueValue();
            referencesTracker.HasUnityReference.Advise(lifetime, b => Enabled.Value = Enabled.Value | b);
        }

        public override LocalList<IDeclaredElement> GetRootMarksFromNode(ITreeNode currentNode, IDeclaredElement containingFunction)
        {
            var result = new LocalList<IDeclaredElement>();
            if (containingFunction == null)
                return result;

            var declaration = currentNode as IDeclaration;
            var declaredElement = declaration?.DeclaredElement;
            if (!ReferenceEquals(containingFunction, declaredElement))
                return result;
            
            var processor = new ExpensiveCodeProcessor();
            declaration.ProcessThisAndDescendants(processor);
            if (processor.IsExpensiveCodeFound)
                result.Add(declaredElement);
            
            return result;
        }

        public override LocalList<IDeclaredElement> GetBanMarksFromNode(ITreeNode currentNode, IDeclaredElement containingFunction)
        {
            return new LocalList<IDeclaredElement>();
        }

        private class ExpensiveCodeProcessor : IRecursiveElementProcessor
        {
            public bool IsExpensiveCodeFound;
            public bool InteriorShouldBeProcessed(ITreeNode element)
            {
                return true;
            }

            public void ProcessBeforeInterior(ITreeNode element)
            {
                switch (element)
                {
                    case IInvocationExpression invocationExpression when
                        PerformanceCriticalCodeStageUtil.IsInvocationExpensive(invocationExpression):
                    case IReferenceExpression referenceExpression when
                        PerformanceCriticalCodeStageUtil.IsCameraMainUsage(referenceExpression):
                    case IAttributesOwnerDeclaration attributesOwnerDeclaration when
                        attributesOwnerDeclaration.DeclaredElement is IAttributesOwner attributesOwner &&
                        PerformanceCriticalCodeStageUtil.HasPerformanceSensitiveAttribute(attributesOwner):
                    {
                        IsExpensiveCodeFound = true;
                        break;
                    }
                }
            }

            public void ProcessAfterInterior(ITreeNode element)
            {
            }

            public bool ProcessingIsFinished => IsExpensiveCodeFound;
        }
    }
}