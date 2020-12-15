using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.Analyses.Bulbs;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using JetBrains.ReSharper.Psi.CSharp.Tree;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.AddPerformanceAnalysisDisableComment
{
    [ContextAction(
        Group = UnityContextActions.GroupID,
        Name = AddPerformanceAnalysisDisableCommentUtil.MESSAGE,
        Description = AddPerformanceAnalysisDisableCommentUtil.MESSAGE,
        Disabled = false,
        AllowedInNonUserFiles = false,
        Priority = 1)]
    public sealed class AddPerformanceAnalysisDisableCommentContextAction : PerformanceExpensiveContextActionBase
    {
        public AddPerformanceAnalysisDisableCommentContextAction([NotNull] ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
        }

        protected override IEnumerable<IntentionAction> GetIntentions(IMethodDeclaration containingMethod)
        {
            return new AddPerformanceAnalysisDisableCommentBulbAction(containingMethod).ToContextActionIntentions();
        }
    }
}