using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Application.DataContext;
using JetBrains.Application.UI.Actions.ActionManager;
using JetBrains.Application.UI.ActionsRevised.Handlers;
using JetBrains.Application.UI.DataContext;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.Collections.Viewable;
using JetBrains.DocumentModel;
using JetBrains.DocumentModel.DataContext;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.DataContext;
using JetBrains.RdBackend.Common.Features.CodeInsights.Providers;
using JetBrains.RdBackend.Common.Features.Services;
using JetBrains.ReSharper.Daemon.CodeInsights;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.Navigation.Settings;
using JetBrains.ReSharper.Plugins.Unity.Core.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Feature.Services.Navigation.GoToUnityUsages;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Plugins.Unity.Rider.Resources;
using JetBrains.ReSharper.Plugins.Unity.Utils;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.DataContext;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Resources.Shell;
using JetBrains.Rider.Model;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.CodeInsights
{
    [ShellComponent]
    public class UnityUsagesCodeVisionProvider : ICodeInsightsProvider
    {
        private readonly IActionManager myActionManager;
        private readonly DataContexts myContexts;

        public UnityUsagesCodeVisionProvider(Shell shell)
        {
            myActionManager = shell.GetComponent<IActionManager>();
            myContexts = shell.GetComponent<DataContexts>();
        }
        
        public bool IsAvailableIn(ISolution solution)
        {
            return solution.GetComponent<UnitySolutionTracker>().IsUnityProject.HasTrueValue();
        }

        public void OnClick(CodeInsightHighlightInfo highlightInfo, ISolution solution)
        {
            var rules = new List<IDataRule>();
            rules.AddRule("Solution", ProjectModelDataConstants.SOLUTION, solution);

            var declaredElement = highlightInfo.CodeInsightsHighlighting.DeclaredElement;
            rules.AddRule("DeclaredElement", PsiDataConstants.DECLARED_ELEMENTS_FROM_ALL_CONTEXTS, new[] {  declaredElement });

            using (ReadLockCookie.Create())
            {
                if (!declaredElement.IsValid())
                    return;

                // Document constant is required for non-empty IFinderSearchRoot
                rules.AddRule("Document", DocumentModelDataConstants.DOCUMENT, highlightInfo.CodeInsightsHighlighting.Range.Document);

                rules.AddRule("DocumentEditorContext", DocumentModelDataConstants.EDITOR_CONTEXT, new DocumentEditorContext(highlightInfo.CodeInsightsHighlighting.Range));
                rules.AddRule("PopupWindowSourceOverride", UIDataConstants.PopupWindowContextSource,
                    new PopupWindowContextSource(lt => new RiderEditorOffsetPopupWindowContext(highlightInfo.CodeInsightsHighlighting.Range.StartOffset.Offset)));

                rules.AddRule("DontNavigateImmediatelyToSingleUsage", NavigationSettings.DONT_NAVIGATE_IMMEDIATELY_TO_SINGLE_USAGE, new object());

                var ctx = myContexts.CreateWithDataRules(Lifetime.Eternal, rules);

                var def = myActionManager.Defs.GetActionDef<GoToUnityUsagesAction>();
                def.EvaluateAndExecute(myActionManager, ctx);
            }
        }

        public void OnExtraActionClick(CodeInsightHighlightInfo highlightInfo, string actionId, ISolution solution)
        {
        }

        public string ProviderId => "Unity Assets Usage";
        public string DisplayName => Strings.UnityUsagesCodeVisionProvider_DisplayName_Unity_assets_usage;
        public CodeVisionAnchorKind DefaultAnchor => CodeVisionAnchorKind.Top;

        public ICollection<CodeVisionRelativeOrdering> RelativeOrderings =>
            new CodeVisionRelativeOrdering[] {new CodeVisionRelativeOrderingBefore(ReferencesCodeInsightsProvider.Id)};
        protected IconId IconId => InsightUnityIcons.InsightUnity.Id;

        public void AddHighlighting(IHighlightingConsumer consumer, IDeclaration declaration,
            IDeclaredElement declaredElement, int count, string tooltipText, string moreText, bool estimatedResult,
            IconModel iconModel)
        {
            consumer.AddHighlighting(new CodeInsightsHighlighting(declaration.GetNameDocumentRange(),
                GetText(count, estimatedResult), tooltipText, moreText, this, declaredElement, iconModel));
        }

        private string GetText(int count, bool estimatedResult)
        {
            return NounUtilEx.ToEmptyPluralOrSingularQuick(count, estimatedResult,
                Strings.UnityUsagesCodeVisionProvider_GetText_No_asset_usages,
                Strings.UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_asset,
                Strings.UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_assets);
        }
    }
}