using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.BurstCodeAnalysis
{
    public abstract class BurstSimpleMethodContextAction : SimpleMethodSingleContextActionBase, IContextAction
    {
        private readonly BurstContextProvider myBurstContextProvider;
        
        protected BurstSimpleMethodContextAction(ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
            myBurstContextProvider = dataProvider.Solution.GetComponent<BurstContextProvider>();
        }

        protected sealed override bool ShouldCreate(IMethodDeclaration methodDeclaration, DaemonProcessKind processKind)
        {
            return myBurstContextProvider.HasContext(methodDeclaration, processKind);
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            return BurstActionsUtil.IsAvailable(CurrentMethodDeclaration);
        }
    }
}