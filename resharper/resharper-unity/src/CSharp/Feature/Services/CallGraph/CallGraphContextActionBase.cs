using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.CallGraph;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

//CGTD check consistency in 211
namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph
{
    public abstract class CallGraphContextActionBase : IContextAction
    {
        [NotNull] private readonly ICSharpContextActionDataProvider myDataProvider;

        protected CallGraphContextActionBase([NotNull] ICSharpContextActionDataProvider dataProvider)
        {
            myDataProvider = dataProvider;
        }

        private IMethodDeclaration ContainingMethod
        {
            get
            {
                // CGTD check consistency in 211
                var methodDeclaration = UnityCallGraphUtil.GetMethodDeclarationByIdentifierOnBothSides(myDataProvider);

                return methodDeclaration;
            }
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            //CGTD check if backend thread
            var containingMethod = ContainingMethod;

            containingMethod?.GetPsiServices().Locks.AssertReadAccessAllowed();

            if (containingMethod == null || !ShouldCreate(containingMethod))
                return EmptyList<IntentionAction>.Instance;

            return GetIntentions(containingMethod);
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            var containingMethod = ContainingMethod;

            containingMethod?.GetPsiServices().Locks.AssertReadAccessAllowed();

            if (containingMethod == null)
                return false;

            // CGTD check if frontend thread
            return IsAvailable(cache, containingMethod);
        }

        protected abstract bool IsAvailable([NotNull] IUserDataHolder holder, [NotNull] IMethodDeclaration methodDeclaration);
        protected abstract bool ShouldCreate([NotNull] IMethodDeclaration containingMethod);
        protected abstract IEnumerable<IntentionAction> GetIntentions([NotNull] IMethodDeclaration containingMethod);
    }
}