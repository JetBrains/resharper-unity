using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using static JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.PerformanceAnalysis.
    PerformanceDisableUtil;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.PerformanceAnalysis
{
    [ContextAction(
        Group = UnityContextActions.GroupID,
        Name = MESSAGE,
        Description = MESSAGE,
        Disabled = false,
        AllowedInNonUserFiles = false,
        Priority = 1)]
    public class PerformanceAnalysisDisableByCommentContextAction : IContextAction
    {
        private readonly SolutionAnalysisService mySwa;
        private readonly PerformanceCriticalContextProvider myPerformanceContextProvider;
        private readonly ExpensiveInvocationContextProvider myExpensiveContextProvider;
        [CanBeNull] private readonly IMethodDeclaration myMethodDeclaration;
        [CanBeNull] private readonly PerformanceAnalysisDisableByCommentBulbAction myBulbAction;

        public PerformanceAnalysisDisableByCommentContextAction(ICSharpContextActionDataProvider dataProvider)
        {
            var identifier = dataProvider.GetSelectedElement<ITreeNode>() as ICSharpIdentifier;

            myMethodDeclaration = MethodDeclarationNavigator.GetByNameIdentifier(identifier);
            myBulbAction = PerformanceAnalysisDisableByCommentBulbAction.Create(myMethodDeclaration);
            mySwa = dataProvider.Solution.GetComponent<SolutionAnalysisService>();
            myPerformanceContextProvider = dataProvider.Solution.GetComponent<PerformanceCriticalContextProvider>();
            myExpensiveContextProvider = dataProvider.Solution.GetComponent<ExpensiveInvocationContextProvider>();
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            if (myMethodDeclaration == null || myBulbAction == null)
                yield break;

            if (!UnityCallGraphUtil.IsSweaCompleted(mySwa))
                yield break;

            if (PerformanceCriticalCodeStageUtil.IsPerformanceCriticalRootMethod(myMethodDeclaration))
                yield break;
            
            var processKind = UnityCallGraphUtil.GetProcessKindForGraph(mySwa);
            var isExpensiveContext = myExpensiveContextProvider.HasContext(myMethodDeclaration, processKind);
            var isPerformanceContext = myPerformanceContextProvider.HasContext(myMethodDeclaration, processKind);

            if (isExpensiveContext || isPerformanceContext)
                yield return myBulbAction.ToContextActionIntention();
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            // CGTD overlook. performance and validity
            if (myMethodDeclaration == null)
                return false;

            var declaredElement = myMethodDeclaration.DeclaredElement;

            return declaredElement != null && myMethodDeclaration.IsValid() &&
                   !PerformanceCriticalCodeStageUtil.IsPerformanceCriticalRootMethod(myMethodDeclaration);
        }
    }
}