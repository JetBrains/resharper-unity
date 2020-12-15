using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.Diagnostics;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph
{
    public abstract class CallGraphQuickFixBase : IQuickFix
    {
        private readonly IMethodDeclaration myContainingBody;

        protected CallGraphQuickFixBase([NotNull] ITreeNode node)
        {
            myContainingBody = node.GetContainingNode<IMethodDeclaration>(returnThis: true);
        }

        protected abstract IEnumerable<IntentionAction> GetBulbItems([NotNull] IMethodDeclaration methodDeclaration);

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            Assertion.AssertNotNull(myContainingBody, "create bulb invoked without checking is available");

            myContainingBody.GetPsiServices().Locks.AssertReadAccessAllowed();
            
            return GetBulbItems(myContainingBody);
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            var containingBody = myContainingBody;
            
            if (containingBody == null)
                return false;
            
            containingBody.GetPsiServices().Locks.AssertReadAccessAllowed();

            return containingBody.IsValid() && IsAvailable(cache, containingBody);
        }

        protected abstract bool IsAvailable([NotNull] IUserDataHolder cache, [NotNull] IMethodDeclaration methodDeclaration);
    }
}