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

        protected string Noun(int count, bool estimatedResult) => "asset usage" + (count == 1 && !estimatedResult ? "" : "s");

        public bool IsAvailableIn(ISolution solution)
        {
            return solution.GetComponent<UnitySolutionTracker>().IsUnityProject.HasTrueValue();
        }

        public void OnClick(CodeInsightsHighlighting highlighting, ISolution solution)
        {
            var rules = new List<IDataRule>();
            rules.AddRule("Solution", ProjectModelDataConstants.SOLUTION, solution);

            var declaredElement = highlighting.DeclaredElement;
            rules.AddRule("DeclaredElement", PsiDataConstants.DECLARED_ELEMENTS_FROM_ALL_CONTEXTS, new[] {  declaredElement });

            using (ReadLockCookie.Create())
            {
                if (!declaredElement.IsValid())
                    return;

                rules.AddRule("DocumentEditorContext", DocumentModelDataConstants.EDITOR_CONTEXT, new DocumentEditorContext(highlighting.Range));
                rules.AddRule("PopupWindowSourceOverride", UIDataConstants.PopupWindowContextSource,
                    new PopupWindowContextSource(lt => new RiderEditorOffsetPopupWindowContext(highlighting.Range.StartOffset.Offset)));

                rules.AddRule("DontNavigateImmediatelyToSingleUsage", NavigationSettings.DONT_NAVIGATE_IMMEDIATELY_TO_SINGLE_USAGE, new object());

                var ctx = myContexts.CreateWithDataRules(Lifetime.Eternal, rules);

                var def = myActionManager.Defs.GetActionDef<GoToUnityUsagesAction>();
                def.EvaluateAndExecute(myActionManager, ctx);
            }
        }

        public void OnExtraActionClick(CodeInsightsHighlighting highlighting, string actionId, ISolution solution)
        {
        }

        public string ProviderId => "Unity Assets Usage";
        public string DisplayName => "Unity assets usage";
        public CodeLensAnchorKind DefaultAnchor => CodeLensAnchorKind.Top;

        public ICollection<CodeLensRelativeOrdering> RelativeOrderings =>
            new CodeLensRelativeOrdering[] {new CodeLensRelativeOrderingBefore(ReferencesCodeInsightsProvider.Id)};
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
            if (count == 0 && !estimatedResult)
                return "No asset usages";

            var countText = count + (estimatedResult ? "+" : "");
            return $"{countText} {Noun(count, estimatedResult)}";
        }
    }
}