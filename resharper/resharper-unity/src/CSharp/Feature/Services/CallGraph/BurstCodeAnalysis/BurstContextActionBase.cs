using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.BurstCodeAnalysis
{
    public abstract class BurstContextActionBase : CallGraphContextActionBase
    {
        private readonly BurstContextProvider myBurstContextProvider;

        protected BurstContextActionBase([NotNull] ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
            myBurstContextProvider = dataProvider.Solution.GetComponent<BurstContextProvider>();
        }

        protected sealed override bool IsAvailable(IUserDataHolder holder, IMethodDeclaration methodDeclaration)
        {
            return BurstActionsUtil.IsAvailable(methodDeclaration);
        }

        protected sealed override bool ShouldCreate(IMethodDeclaration containingMethod)
        {
            return myBurstContextProvider.IsMarkedSwea(containingMethod);
        }
    }
}