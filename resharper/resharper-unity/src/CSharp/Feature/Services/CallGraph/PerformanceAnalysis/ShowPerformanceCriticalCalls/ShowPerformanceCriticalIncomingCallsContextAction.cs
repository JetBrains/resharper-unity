using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Features.Inspections.CallHierarchy;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.ShowPerformanceCriticalCalls
{
    [ContextAction(
        Group = UnityContextActions.GroupID,
        Name = ShowPerformanceCriticalCallsUtil.INCOMING_CALLS_MESSAGE,
        Description = ShowPerformanceCriticalCallsUtil.INCOMING_CALLS_MESSAGE,
        Disabled = false,
        AllowedInNonUserFiles = false,
        Priority = 1)]
    public class ShowPerformanceCriticalIncomingCallsContextAction : PerformanceOnlyContextActionBase
    {
        public ShowPerformanceCriticalIncomingCallsContextAction([NotNull] ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }

        protected override IEnumerable<IntentionAction> GetIntentions(IMethodDeclaration containingMethod)
        {
            var actions = ShowPerformanceCriticalIncomingCallsBulbAction.GetAllCalls(containingMethod);
            return actions.Select(action =>
                action.ToContextActionIntention(IntentionsAnchors.ConfigureActionsAnchor, action.Icon)).ToArray();
        }
    }
}