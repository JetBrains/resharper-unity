using JetBrains.Application.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CSharp.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph;
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

        public ExpensiveCodeCallGraphAnalyzer(Lifetime lifetime, ISolution solution,
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
            
            if (containingFunction == null)
                return result;

            var declaration = currentNode as IDeclaration;
            var declaredElement = declaration?.DeclaredElement;
            
            if (!ReferenceEquals(containingFunction, declaredElement))
                return result;

            if (containingFunction is IAttributesSet attributesSet &&
                attributesSet.HasAttributeInstance(CallGraphActionUtil.ExpensiveCodeAnalysisEnableAttribute,
                    AttributesSource.Self))
            {
                result.Add(containingFunction);
                return result;
            }

            var processor = new ExpensiveCodeProcessor();
            
            declaration.ProcessDescendants(processor);
            
            if (processor.IsExpensiveCodeFound)
                result.Add(declaredElement);

            return result;
        }

        public override LocalList<IDeclaredElement> GetBanMarksFromNode(ITreeNode currentNode,
            IDeclaredElement containingFunction)
        {
            var result = new LocalList<IDeclaredElement>();
            
            if (containingFunction == null)
                return result;

            var declaration = currentNode as IDeclaration;
            var declaredElement = declaration?.DeclaredElement;
            
            if (!ReferenceEquals(containingFunction, declaredElement))
                return result;
            
            if (containingFunction is IAttributesSet attributesSet &&
                attributesSet.HasAttributeInstance(CallGraphActionUtil.ExpensiveCodeAnalysisDisableAttribute,
                    AttributesSource.Self))
                result.Add(containingFunction);
            
            return result;
        }

        private class ExpensiveCodeProcessor : IRecursiveElementProcessor
        {
            public bool IsExpensiveCodeFound;
            private readonly SeldomInterruptChecker myInterruptChecker = new SeldomInterruptChecker();

            public bool InteriorShouldBeProcessed(ITreeNode element)
            {
                myInterruptChecker.CheckForInterrupt();
                switch (element)
                {
                    case IFunctionDeclaration _:
                    case ICSharpClosure _:
                        return false;
                    default:
                        return true;
                }
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