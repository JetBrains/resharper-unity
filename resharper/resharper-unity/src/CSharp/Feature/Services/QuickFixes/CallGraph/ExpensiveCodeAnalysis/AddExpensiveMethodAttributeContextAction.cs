using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.Collections;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using static JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.ExpensiveCodeAnalysis.ExpensiveCodeActionsUtil;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.ExpensiveCodeAnalysis
{
    [ContextAction(
        Group = UnityContextActions.GroupID,
        Name = MESSAGE,
        Description = MESSAGE,
        Disabled = false,
        AllowedInNonUserFiles = false,
        Priority = 1)]
    public sealed class AddExpensiveMethodAttributeContextAction : AddExpensiveMethodAttributeActionBase
    {
        private readonly SolutionAnalysisService mySwa;
        private readonly UnityProblemAnalyzerContextSystem myUnityProblemAnalyzerContextSystem;
        private readonly IContextBoundSettingsStore mySettingsStore;
        private readonly ExpensiveInvocationContextProvider myExpensiveContextProvider;

        public AddExpensiveMethodAttributeContextAction(ICSharpContextActionDataProvider dataProvider)
        {
            var identifier = dataProvider.GetSelectedElement<ITreeNode>() as ICSharpIdentifier;

            MethodDeclaration = MethodDeclarationNavigator.GetByNameIdentifier(identifier);
            FixedArguments = GetExpensiveAttributeValues(MethodDeclaration);
            mySwa = dataProvider.Solution.GetComponent<SolutionAnalysisService>();
            myUnityProblemAnalyzerContextSystem =
                dataProvider.Solution.GetComponent<UnityProblemAnalyzerContextSystem>();
            mySettingsStore = dataProvider.Solution.GetSettingsStore();
            myExpensiveContextProvider = dataProvider.Solution.GetComponent<ExpensiveInvocationContextProvider>();
        }

        protected override IMethodDeclaration MethodDeclaration { get; }
        protected override CompactList<AttributeValue> FixedArguments { get; }

        public override IEnumerable<IntentionAction> CreateBulbItems()
        {
            if (!UnityCallGraphUtil.IsSweaCompleted(mySwa))
                yield break;

            if (PerformanceCriticalCodeStageUtil.IsPerformanceCriticalRootMethod(MethodDeclaration))
                yield break;
            
            var processKind = UnityCallGraphUtil.GetProcessKindForGraph(mySwa);
            
            if (myExpensiveContextProvider.IsMarked(MethodDeclaration, processKind, false))
                yield break;

            var performanceContextProvider = myUnityProblemAnalyzerContextSystem.GetContextProvider(mySettingsStore,
                UnityProblemAnalyzerContextElement.PERFORMANCE_CONTEXT);

            var isPerformanceContext =
                performanceContextProvider.IsMarked(MethodDeclaration, processKind, false);

            if (isPerformanceContext)
                yield return this.ToContextActionIntention();
        }
    }
}