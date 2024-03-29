using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Analyzers;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.BurstCodeAnalysis
{
    public abstract class BurstBulbItemsProvider : SimpleCallGraphBulbItemsProviderBase, IBurstBulbItemsProvider
    {
        private readonly BurstContextProvider myBurstContextProvider;

        protected BurstBulbItemsProvider(ISolution solution, BurstContextProvider burstContextProvider) : base(solution)
        {
            myBurstContextProvider = burstContextProvider;
        }
        protected override bool CheckCallGraph(IMethodDeclaration methodDeclaration, IReadOnlyCallGraphContext context)
        {
            var declaredElement = methodDeclaration.DeclaredElement;
            
            return myBurstContextProvider.IsMarkedStage(declaredElement, context);
        }
    }
}