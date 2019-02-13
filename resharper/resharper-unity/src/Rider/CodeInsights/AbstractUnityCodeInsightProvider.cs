using System.Collections.Generic;
using JetBrains.Application.UI.Controls.GotoByName;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.Core;
using JetBrains.Platform.RdFramework;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CodeInsights;
using JetBrains.ReSharper.Host.Features.TextControls;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.ReSharper.Psi;
using JetBrains.Rider.Model;
using JetBrains.TextControl.TextControlsManagement;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights
{
    public abstract class AbstractUnityCodeInsightProvider : ICodeInsightsProvider
    {
        public static string StartUnityActionId => "startUnity";

        private readonly UnityHost myHost;
        private readonly BulbMenuComponent myBulbMenu;

        protected AbstractUnityCodeInsightProvider(UnityHost host, BulbMenuComponent bulbMenu)
        {
            myHost = host;
            myBulbMenu = bulbMenu;
        }

        public void OnClick(CodeInsightsHighlighting highlighting)
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

        public void OnExtraActionClick(CodeInsightsHighlighting highlighting, string actionId)
        {
           if (actionId.Equals(StartUnityActionId))
           {
               myHost.PerformModelAction(model => model.StartUnity.Fire(Unit.Instance));
           }
        }

        public virtual IconId IconId => InsightUnityIcons.InsightUnity.Id;

        public abstract string ProviderId { get; }
        public abstract string DisplayName { get; }
        public abstract CodeLensAnchorKind DefaultAnchor { get; }
        public abstract ICollection<CodeLensRelativeOrdering> RelativeOrderings { get; }
    }
}