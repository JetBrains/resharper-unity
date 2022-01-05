using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Feature.Services.QuickFixes;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Errors;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.AddPerformanceAnalysisDisableComment
{
    [QuickFix]
    public sealed class AddPerformanceAnalysisDisableCommentQuickFix : CallGraphQuickFixBase
    {
        public AddPerformanceAnalysisDisableCommentQuickFix([NotNull] UnityPerformanceInvocationWarning performanceHighlighting)
            : base(performanceHighlighting.InvocationExpression)
        {
        }

        protected override IEnumerable<IntentionAction> GetBulbItems(IMethodDeclaration methodDeclaration)
        {
            return new AddPerformanceAnalysisDisableCommentBulbAction(methodDeclaration).ToQuickFixIntentions();
        }

        protected override bool IsAvailable(IUserDataHolder cache, IMethodDeclaration methodDeclaration)
        {
            return PerformanceAnalysisUtil.IsAvailable(methodDeclaration);
        }
    }
}