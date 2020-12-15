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

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.BurstCodeAnalysis.ShowBurstCalls
{
    [ContextAction(
        Group = UnityContextActions.GroupID,
        Name = ShowBurstCallsUtil.CONTEXT_ACTION_DESCRIPTION,
        Description = ShowBurstCallsUtil.CONTEXT_ACTION_DESCRIPTION,
        Disabled = false,
        AllowedInNonUserFiles = false,
        Priority = 1)]
    public sealed class ShowBurstCallsContextAction : BurstContextActionBase
    {
        public ShowBurstCallsContextAction([NotNull] ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }

        protected override IEnumerable<IntentionAction> GetIntentions(IMethodDeclaration containingMethod)
        {
            var actions = ShowBurstCallsBulbAction.GetAllCalls(containingMethod);
            
            return actions.Select(action => action.ToContextActionIntention(
                IntentionsAnchors.ContextActionsAnchor, action.Icon)).ToArray();
        }
    }
}