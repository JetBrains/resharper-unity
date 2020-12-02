using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Threading;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis
{
    [QuickFix]
    public sealed class PerformanceAnalysisDisableByCommentQuickFix : IQuickFix
    {
        [CanBeNull] private readonly IMethodDeclaration myMethodDeclaration;
        [CanBeNull] private readonly PerformanceAnalysisDisableByCommentBulbAction myBulbAction;

        public PerformanceAnalysisDisableByCommentQuickFix(UnityPerformanceInvocationWarning performanceHighlighting)
        {
            myMethodDeclaration = performanceHighlighting.InvocationExpression?.GetContainingNode<IMethodDeclaration>();
            
            if (myMethodDeclaration != null)
                myBulbAction = new PerformanceAnalysisDisableByCommentBulbAction(myMethodDeclaration);
        }

        public IEnumerable<IntentionAction> CreateBulbItems()
        {
            if (myMethodDeclaration == null || myBulbAction == null)
                return EmptyList<IntentionAction>.Instance;
            
            myMethodDeclaration.GetPsiServices().Locks.AssertReadAccessAllowed();

            return myBulbAction.ToQuickFixIntentions();
        }

        public bool IsAvailable(IUserDataHolder cache)
        {
            return PerformanceDisableUtil.IsAvailable(myMethodDeclaration);
        }
    }
}