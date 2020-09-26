using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
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
        private readonly UnityProblemAnalyzerContextSystem myUnityProblemAnalyzerContextSystem;
        private readonly SolutionAnalysisService mySwa;

        public AddDiscardAttributeContextAction(ICSharpContextActionDataProvider dataProvider)
        {
            mySwa = dataProvider.Solution.GetComponent<SolutionAnalysisService>();
            myUnityProblemAnalyzerContextSystem =
                dataProvider.Solution.GetComponent<UnityProblemAnalyzerContextSystem>();
            mySettingsStore = dataProvider.Solution.GetSettingsStore();

            var identifier = dataProvider.GetSelectedElement<ITreeNode>() as ICSharpIdentifier;

            MethodDeclaration = MethodDeclarationNavigator.GetByNameIdentifier(identifier);
        }

        protected override IMethodDeclaration MethodDeclaration { get; }

        public override IEnumerable<IntentionAction> CreateBulbItems()
        {
            if (mySwa.Configuration?.Enabled?.Value == false)
                yield break;
            
            var burstContextProvider = myUnityProblemAnalyzerContextSystem.GetContextProvider(mySettingsStore,
                UnityProblemAnalyzerContextElement.BURST_CONTEXT);
            var processKind = CallGraphActionUtil.GetProcessKind(mySwa);
            var isBurstContext = burstContextProvider.IsMarked(MethodDeclaration, processKind, false);

            if (isBurstContext)
                yield return this.ToContextActionIntention();
        }
    }
}