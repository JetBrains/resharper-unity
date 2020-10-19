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
using static JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.ExpensiveCodeAnalysis.
    ExpensiveCodeActionsUtil;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.ExpensiveCodeAnalysis.
    AddExpensiveComment
{
    [ContextAction(
        Group = UnityContextActions.GroupID,
        Name = MESSAGE,
        Description = MESSAGE,
        Disabled = false,
        AllowedInNonUserFiles = false,
        Priority = 1)]
    public sealed class AddExpensiveCommentContextAction : IContextAction
    {
        [NotNull] private readonly SolutionAnalysisService mySwa;
        [NotNull] private readonly PerformanceCriticalContextProvider myPerformanceContextProvider;
        [NotNull] private readonly ExpensiveInvocationContextProvider myExpensiveContextProvider;
        [NotNull] private readonly ICSharpContextActionDataProvider myDataProvider;
     
        public AddExpensiveCommentContextAction(ICSharpContextActionDataProvider dataProvider)
        {
            myDataProvider = dataProvider;
            mySwa = dataProvider.Solution.GetComponent<SolutionAnalysisService>();
            myPerformanceContextProvider = dataProvider.Solution.GetComponent<PerformanceCriticalContextProvider>();
            myExpensiveContextProvider = dataProvider.Solution.GetComponent<ExpensiveInvocationContextProvider>();
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            var identifier = myDataProvider.GetSelectedElement<ITreeNode>() as ICSharpIdentifier;
            var methodDeclaration = MethodDeclarationNavigator.GetByNameIdentifier(identifier);
            
            if (methodDeclaration == null)
                return EmptyList<IntentionAction>.Instance;

            if (!UnityCallGraphUtil.IsSweaCompleted(mySwa))
                return EmptyList<IntentionAction>.Instance;

            var processKind = UnityCallGraphUtil.GetProcessKindForGraph(mySwa);

            if (myExpensiveContextProvider.HasContext(methodDeclaration, processKind))
                return EmptyList<IntentionAction>.Instance;

            var isPerformanceContext = myPerformanceContextProvider.HasContext(methodDeclaration, processKind);

            if (!isPerformanceContext)
                return EmptyList<IntentionAction>.Instance;
            
            var bulbAction = new AddExpensiveCommentBulbAction(methodDeclaration);
            
            return bulbAction.ToContextActionIntentions();
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            var identifier = myDataProvider.GetSelectedElement<ITreeNode>() as ICSharpIdentifier;
            var methodDeclaration = MethodDeclarationNavigator.GetByNameIdentifier(identifier);
            
            if (methodDeclaration == null)
                return false;

            var declaredElement = methodDeclaration.DeclaredElement;

            return declaredElement != null && methodDeclaration.IsValid() &&
                   !PerformanceCriticalCodeStageUtil.IsPerformanceCriticalRootMethod(methodDeclaration);
        }
    }
}