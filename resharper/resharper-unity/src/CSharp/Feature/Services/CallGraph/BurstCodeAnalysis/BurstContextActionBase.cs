using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.BurstCodeAnalysis
{
    public abstract class BurstContextActionBase : CallGraphContextActionBase
    {
        private readonly BurstContextProvider myBurstContextProvider;
        private readonly SolutionAnalysisService mySolutionAnalysisService;

        protected BurstContextActionBase([NotNull] ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
            myBurstContextProvider = dataProvider.Solution.GetComponent<BurstContextProvider>();
            mySolutionAnalysisService = dataProvider.Solution.GetComponent<SolutionAnalysisService>();
        }

        protected override bool IsAvailable(IUserDataHolder holder, IMethodDeclaration methodDeclaration)
        {
            return BurstActionsUtil.IsAvailable(methodDeclaration);
        }

        protected sealed override bool ShouldCreate(IMethodDeclaration containingMethod)
        {
            var declaredElement = containingMethod.DeclaredElement;
            
            return myBurstContextProvider.IsMarkedSweaDependent(declaredElement, mySolutionAnalysisService);
        }
    }
}