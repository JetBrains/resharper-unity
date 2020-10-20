using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis.
    AddDiscardAttribute
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
        [NotNull] private readonly BurstContextProvider myBurstContextProvider;
        [NotNull] private readonly SolutionAnalysisService mySwa;
        [NotNull] private readonly ICSharpContextActionDataProvider myDataProvider;

        public AddDiscardAttributeContextAction([NotNull] ICSharpContextActionDataProvider dataProvider)
        {
            myDataProvider = dataProvider;

            mySwa = dataProvider.Solution.GetComponent<SolutionAnalysisService>();
            myBurstContextProvider = dataProvider.Solution.GetComponent<BurstContextProvider>();
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            var identifier = myDataProvider.GetSelectedElement<ITreeNode>() as ICSharpIdentifier;
            var methodDeclaration = MethodDeclarationNavigator.GetByNameIdentifier(identifier);

            if (methodDeclaration == null)
                return EmptyList<IntentionAction>.Instance;

            if (!UnityCallGraphUtil.IsSweaCompleted(mySwa))
                return EmptyList<IntentionAction>.Instance;

            var bulbAction = new AddDiscardAttributeBulbAction(methodDeclaration);
            var processKind = UnityCallGraphUtil.GetProcessKindForGraph(mySwa);
            var isBurstContext = myBurstContextProvider.HasContext(methodDeclaration, processKind);

            return isBurstContext
                ? bulbAction.ToContextActionIntentions()
                : EmptyList<IntentionAction>.Instance;
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            var identifier = myDataProvider.GetSelectedElement<ITreeNode>() as ICSharpIdentifier;
            var methodDeclaration = MethodDeclarationNavigator.GetByNameIdentifier(identifier);

            return AddDiscardAttributeUtil.IsAvailable(methodDeclaration);
        }
    }
}