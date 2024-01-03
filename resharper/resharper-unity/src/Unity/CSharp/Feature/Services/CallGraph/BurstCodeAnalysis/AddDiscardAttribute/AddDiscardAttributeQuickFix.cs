using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Highlightings;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.BurstCodeAnalysis.AddDiscardAttribute
{
    // [QuickFix] 
    // removed due to possible uselessness 
    public sealed class AddDiscardAttributeQuickFix : CallGraphQuickFixBase
    {
        public AddDiscardAttributeQuickFix([NotNull] IBurstHighlighting burstHighlighting)
            : base(burstHighlighting.Node)
        {
        }

        protected override IEnumerable<IntentionAction> GetBulbItems(IMethodDeclaration methodDeclaration)
        {
            return new AddDiscardAttributeBulbAction(methodDeclaration).ToQuickFixIntentions();
        }

        protected override bool IsAvailable(IUserDataHolder cache, IMethodDeclaration methodDeclaration)
        {
            return BurstActionsUtil.IsAvailable(methodDeclaration);
        }
    }
}