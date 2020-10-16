using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis.AddDiscardAttribute
{
    [ContextAction(
        Group = UnityContextActions.GroupID,
        Name = AddDiscardAttributeUtil.DiscardActionMessage,
        Description = AddDiscardAttributeUtil.DiscardActionMessage,
        Disabled = false,
        AllowedInNonUserFiles = false,
        Priority = 1)]
    public sealed class AddDiscardAttributeContextAction : IContextAction
    {
        private readonly BurstContextProvider myBurstContextProvider;
        private readonly SolutionAnalysisService mySwa;
        [CanBeNull] private readonly IMethodDeclaration myMethodDeclaration;
        [CanBeNull] private readonly AddDiscardAttributeBulbAction myBulbAction;

        public AddDiscardAttributeContextAction([NotNull] ICSharpContextActionDataProvider dataProvider)
        {
            //CGTD overlook. 2 function to select
            var identifier = dataProvider.GetSelectedElement<ITreeNode>() as ICSharpIdentifier;

            mySwa = dataProvider.Solution.GetComponent<SolutionAnalysisService>();
            myBurstContextProvider = dataProvider.Solution.GetComponent<BurstContextProvider>();
            myMethodDeclaration = MethodDeclarationNavigator.GetByNameIdentifier(identifier);
            myBulbAction = AddDiscardAttributeBulbAction.Create(myMethodDeclaration);
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            if (myMethodDeclaration == null || myBulbAction == null)
                yield break;
            
            if (!UnityCallGraphUtil.IsSweaCompleted(mySwa))
                yield break;
            
            var processKind = UnityCallGraphUtil.GetProcessKindForGraph(mySwa);
            var isBurstContext = myBurstContextProvider.HasContext(myMethodDeclaration, processKind);

            if (isBurstContext)
                yield return myBulbAction.ToContextActionIntention();
        }

        public bool IsAvailable(IUserDataHolder cache) => AddDiscardAttributeUtil.IsAvailable(myMethodDeclaration);
    }
}