using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis.AddDiscardAttribute;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis
{
    public abstract class SimpleBurstContextAction : SimpleMethodContextActionBase, IContextAction
    {
        private readonly BurstContextProvider myBurstContextProvider;
        
        protected SimpleBurstContextAction(ICSharpContextActionDataProvider dataProvider)
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