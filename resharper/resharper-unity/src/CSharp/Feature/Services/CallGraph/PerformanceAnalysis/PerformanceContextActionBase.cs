using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis
{
    public abstract class PerformanceContextActionBase : PerformanceAnalysisContextActionBase
    {
        protected PerformanceContextActionBase([NotNull] ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }

        protected override bool ShouldCreate(IMethodDeclaration containingMethod)
        {
            var declaredElement = containingMethod.DeclaredElement;
            var isPerformanceContext = PerformanceContextProvider.IsMarkedSweaDependent(declaredElement, SolutionAnalysisService);

            return isPerformanceContext;
        }
    }
}