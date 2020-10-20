using System.Collections.Generic;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.Application.UI.Controls.GotoByName;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CodeInsights;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Host.Features.TextControls;
using JetBrains.ReSharper.Plugins.Unity.Rider.Protocol;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Rider.Model;
using JetBrains.TextControl.TextControlsManagement;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights
{
    public abstract class AbstractUnityCodeInsightProvider : ICodeInsightsProvider
    {
        public static string StartUnityActionId => "startUnity";

        private readonly FrontendBackendHost myFrontendBackendHost;
        private readonly BulbMenuComponent myBulbMenu;

        protected AbstractUnityCodeInsightProvider(FrontendBackendHost frontendBackendHost, BulbMenuComponent bulbMenu)
        {
            myFrontendBackendHost = frontendBackendHost;
            myBulbMenu = bulbMenu;
        }

        public virtual void OnClick(CodeInsightsHighlighting highlighting, ISolution solution)
        {
            var windowContextSource = new PopupWindowContextSource(
                lt => new HostTextControlPopupWindowContext(lt,
                    highlighting.DeclaredElement.GetSolution().GetComponent<TextControlManager>().LastFocusedTextControl
                        .Value).MarkAsOriginatedFromDataContext());
            if (highlighting is UnityCodeInsightsHighlighting unityCodeInsightsHighlighting)
            {
                if (unityCodeInsightsHighlighting.MenuItems.Count > 0)
                    myBulbMenu.ShowBulbMenu(unityCodeInsightsHighlighting.MenuItems, windowContextSource);
            }
        }

        public void OnExtraActionClick(CodeInsightsHighlighting highlighting, string actionId, ISolution solution)
        {
            if (actionId.Equals(StartUnityActionId))
            {
                myFrontendBackendHost.Do(model => model.StartUnity());
            }
        }

        // TODO: Fix sdk and add correct check
        // We could not check that our provider is available while solution is loading, because user could add Unity Engine
        // reference later. wait sdk update
        public bool IsAvailableIn(ISolution solution) => true;

        public virtual void AddHighlighting(IHighlightingConsumer consumer, ICSharpDeclaration element,
            IDeclaredElement declaredElement, string displayName, string tooltip, string moreText, IconModel iconModel,
            IEnumerable<BulbMenuItem> items, List<CodeLensEntryExtraActionModel> extraActions)
        {
            consumer.AddHighlighting(new UnityCodeInsightsHighlighting(element.GetNameDocumentRange(),
                displayName, tooltip, moreText, this, declaredElement, iconModel, items,
                extraActions));
        }

        public abstract string ProviderId { get; }
        public abstract string DisplayName { get; }
        public abstract CodeLensAnchorKind DefaultAnchor { get; }
        public abstract ICollection<CodeLensRelativeOrdering> RelativeOrderings { get; }
    }
}