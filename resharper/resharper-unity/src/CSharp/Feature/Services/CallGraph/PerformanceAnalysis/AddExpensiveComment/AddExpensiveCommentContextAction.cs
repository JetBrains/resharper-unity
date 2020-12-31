using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.Application.Settings;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.ContextActions;
using JetBrains.ReSharper.Plugins.Unity.Settings;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.CallGraph.PerformanceAnalysis.AddExpensiveComment
{
    [ContextAction(
        Group = UnityContextActions.GroupID,
        Name = AddExpensiveCommentUtil.MESSAGE,
        Description = AddExpensiveCommentUtil.MESSAGE,
        Disabled = false,
        AllowedInNonUserFiles = false,
        Priority = 1)]
    public sealed class AddExpensiveCommentContextAction : PerformanceOnlyContextActionBase
    {
        private readonly IContextBoundSettingsStoreLive mySettingStore;
        
        public AddExpensiveCommentContextAction([NotNull] ICSharpContextActionDataProvider dataProvider) : base(dataProvider)
        {
            mySettingStore = dataProvider.Solution.GetComponent<IApplicationWideContextBoundSettingStore>().BoundSettingsStore;
        }

        protected override IEnumerable<IntentionAction> GetIntentions(IMethodDeclaration containingMethod)
        {
            return new AddExpensiveCommentBulbAction(containingMethod).ToContextActionIntentions();
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