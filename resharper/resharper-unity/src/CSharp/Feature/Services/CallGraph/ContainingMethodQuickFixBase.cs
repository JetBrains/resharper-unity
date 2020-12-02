using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph
{
    public interface IBulbActionProvider<out T> where T : IBulbAction
    {
        T GetBulbAction([NotNull] IMethodDeclaration methodDeclaration);
    }

    public abstract class ContainingMethodQuickFixBase<T1, T2> where T2 : IBulbAction where T1 : struct, IBulbActionProvider<T2>
    {
        [CanBeNull] protected readonly IMethodDeclaration MethodDeclaration;
        [CanBeNull] private readonly T2 myBulbAction;

        protected ContainingMethodQuickFixBase([CanBeNull] ITreeNode node)
        {
            MethodDeclaration = node?.GetContainingNode<IMethodDeclaration>();

            if (MethodDeclaration == null)
                return;
                
            var bulbActionProvider = new T1();
            myBulbAction = bulbActionProvider.GetBulbAction(MethodDeclaration);
        }
        
        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            var methodDeclaration = MethodDeclaration;
            var bulbAction = myBulbAction;
            
            if (methodDeclaration == null || bulbAction == null)
                return EmptyList<IntentionAction>.Instance;
            
            methodDeclaration.GetPsiServices().Locks.AssertReadAccessAllowed();
            
            return bulbAction.ToQuickFixIntentions();
        }
    }
}