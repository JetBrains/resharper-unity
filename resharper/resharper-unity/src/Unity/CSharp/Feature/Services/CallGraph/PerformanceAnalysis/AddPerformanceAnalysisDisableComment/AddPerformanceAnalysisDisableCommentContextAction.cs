using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using JetBrains.ReSharper.Plugins.Unity.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.AddPerformanceAnalysisDisableComment
{
    [ContextAction(
        GroupType = typeof(CSharpUnityContextActions),
        ResourceType = typeof(Strings), NameResourceName = nameof(Strings.AddPerformanceAnalysisDisableCommentContextAction_Name), 
        DescriptionResourceName = nameof(Strings.AddPerformanceAnalysisDisableCommentContextAction_Name),
        Disabled = false,
        AllowedInNonUserFiles = false,
        Priority = 1)]
    public sealed class AddPerformanceAnalysisDisableCommentContextAction : PerformanceOrExpensiveContextActionBase
    {
        private readonly IContextBoundSettingsStoreLive mySettingStore;

        public AddPerformanceAnalysisDisableCommentContextAction([NotNull] ICSharpContextActionDataProvider dataProvider)
            : base(dataProvider)
        {
            mySettingStore = dataProvider.Solution.GetComponent<IApplicationWideContextBoundSettingStore>().BoundSettingsStore;
        }

        protected override IEnumerable<IntentionAction> GetIntentions(IMethodDeclaration containingMethod)
        {
            return new AddPerformanceAnalysisDisableCommentBulbAction(containingMethod).ToContextActionIntentions();
        }

        protected override bool IsAvailable(IUserDataHolder cache, IMethodDeclaration containingMethod)
        {
            var isHotIconsEnabled = mySettingStore.GetValue((UnitySettings key) => key.EnableIconsForPerformanceCriticalCode);

            if (isHotIconsEnabled)
                return false;
            
            return base.IsAvailable(cache, containingMethod);
        }
    }
}