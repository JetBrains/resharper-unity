using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis
{
    [ContextAction(
        Group = UnityContextActions.GroupID,
        Name = Message,
        Description = Message,
        Disabled = false,
        AllowedInNonUserFiles = false,
        Priority = 1)]
    public sealed class
        AddDiscardAttributeContextAction : AddDiscardAttributeActionBase
    {
        private readonly IContextBoundSettingsStore mySettingsStore;
        private readonly BurstContextProvider myBurstContextProvider;
        private readonly SolutionAnalysisService mySwa;

        public AddDiscardAttributeContextAction(ICSharpContextActionDataProvider dataProvider)
        {
            mySwa = dataProvider.Solution.GetComponent<SolutionAnalysisService>();
            myBurstContextProvider =
                dataProvider.Solution.GetComponent<BurstContextProvider>();
            mySettingsStore = dataProvider.Solution.GetSettingsStore();

            var identifier = dataProvider.GetSelectedElement<ITreeNode>() as ICSharpIdentifier;

            MethodDeclaration = MethodDeclarationNavigator.GetByNameIdentifier(identifier);
        }

        protected override IMethodDeclaration MethodDeclaration { get; }

        public override IEnumerable<IntentionAction> CreateBulbItems()
        {
            if (!UnityCallGraphUtil.IsSweaCompleted(mySwa))
                yield break;
            
            var processKind = UnityCallGraphUtil.GetProcessKindForGraph(mySwa);
            var isBurstContext = myBurstContextProvider.HasContext(MethodDeclaration, processKind);

            if (isBurstContext)
                yield return this.ToContextActionIntention();
        }
    }
}