using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Features.Inspections.CallHierarchy;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.ShowPerformanceCriticalCalls;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.ShowExpensiveCalls.Incoming
{
    [ContextAction(
        Group = UnityContextActions.GroupID,
        Name = ShowExpensiveCallsUtil.INCOMING_MESSAGE,
        Description = ShowExpensiveCallsUtil.INCOMING_MESSAGE,
        Disabled = false,
        AllowedInNonUserFiles = false,
        Priority = 1)]
    public class ShowExpensiveIncomingCallsContextAction : ExpensiveContextActionBase
    {
        public ShowExpensiveIncomingCallsContextAction([NotNull] ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }

        protected override IEnumerable<IntentionAction> GetIntentions(IMethodDeclaration containingMethod)
        {
            return new ShowExpensiveIncomingCallsBulbAction(containingMethod)
                .ToContextActionIntentions(IntentionsAnchors.ContextActionsAnchor, CallHierarchyIcons.CalledByArrow);
        }
    }
}