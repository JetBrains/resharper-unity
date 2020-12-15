using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Features.Inspections.CallHierarchy;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.BurstCodeAnalysis.ShowBurstCalls.Outgoing
{
    [ContextAction(
        Group = UnityContextActions.GroupID,
        Name = ShowBurstCallsUtil.OUTGOING_MESSAGE,
        Description = ShowBurstCallsUtil.OUTGOING_MESSAGE,
        Disabled = false,
        AllowedInNonUserFiles = false,
        Priority = 1)]
    public sealed class ShowBurstOutgoingCallsContextAction : BurstContextActionBase
    {
        public ShowBurstOutgoingCallsContextAction([NotNull] ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }

        protected override IEnumerable<IntentionAction> GetIntentions(IMethodDeclaration containingMethod)
        {
            return new ShowBurstOutgoingCallsBulbAction(containingMethod).ToContextActionIntentions(
                IntentionsAnchors.ContextActionsAnchor,
                CallHierarchyIcons.CalledArrow);
        }
    }
}