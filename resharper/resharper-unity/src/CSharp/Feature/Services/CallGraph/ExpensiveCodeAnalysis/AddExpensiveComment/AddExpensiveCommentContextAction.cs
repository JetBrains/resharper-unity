using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.ContextSystem;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;
using static JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.ExpensiveCodeAnalysis.ExpensiveCodeActionsUtil;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.ExpensiveCodeAnalysis.
    AddExpensiveComment
{
    [ContextAction(
        Group = UnityContextActions.GroupID,
        Name = MESSAGE,
        Description = MESSAGE,
        Disabled = false,
        AllowedInNonUserFiles = false,
        Priority = 1)]
    public sealed class AddExpensiveCommentContextAction : SimpleMethodContextActionBase, IContextAction
    {
        [NotNull] private readonly PerformanceCriticalContextProvider myPerformanceContextProvider;
        [NotNull] private readonly ExpensiveInvocationContextProvider myExpensiveContextProvider;

        public AddExpensiveCommentContextAction(ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
            myPerformanceContextProvider = dataProvider.Solution.GetComponent<PerformanceCriticalContextProvider>();
            myExpensiveContextProvider = dataProvider.Solution.GetComponent<ExpensiveInvocationContextProvider>();
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            var methodDeclaration = CurrentMethodDeclaration;
            
            return PerformanceDisableUtil.IsAvailable(methodDeclaration);
        }

        protected override bool ShouldCreate(IMethodDeclaration methodDeclaration, DaemonProcessKind processKind)
        {
            var isExpensiveContext = myExpensiveContextProvider.HasContext(methodDeclaration, processKind);
            var isPerformanceContext = myPerformanceContextProvider.HasContext(methodDeclaration, processKind);

            return isPerformanceContext && !isExpensiveContext;
        }

        protected override IEnumerable<IntentionAction> GetActions(IMethodDeclaration methodDeclaration)
        {
            return new AddExpensiveCommentBulbAction(methodDeclaration).ToContextActionIntentions();
        }
    }
}