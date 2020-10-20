using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.BurstCodeAnalysis.Highlightings;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.QuickFixes.CallGraph.BurstCodeAnalysis.
    AddDiscardAttribute
{
    [QuickFix]
    public sealed class AddDiscardAttributeQuickFix : IQuickFix
    {
        [CanBeNull] private readonly IMethodDeclaration myMethodDeclaration;
        [CanBeNull] private readonly AddDiscardAttributeBulbAction myBulbAction;
        public AddDiscardAttributeQuickFix(IBurstHighlighting burstHighlighting)
        {
            myMethodDeclaration = burstHighlighting?.Node?.GetContainingNode<IMethodDeclaration>();

            if (myMethodDeclaration != null)
                myBulbAction = new AddDiscardAttributeBulbAction(myMethodDeclaration);
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            if (myMethodDeclaration == null || myBulbAction == null)
                return EmptyList<IntentionAction>.Instance;;
            
            return myBulbAction.ToQuickFixIntentions();
        }


        public bool IsAvailable(IUserDataHolder cache) => AddDiscardAttributeUtil.IsAvailable(myMethodDeclaration);
    }
}