using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.UI.Controls.GotoByName;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.Platform.RdFramework;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CodeInsights;
using JetBrains.ReSharper.Host.Features.TextControls;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Psi;
using JetBrains.Rider.Model;
using JetBrains.TextControl.TextControlsManagement;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights
{
    public abstract class AbstractUnityCodeInsightProvider : ICodeInsightsProvider
    {
        public static string StartUnityActionId => "startUnity";
        public IconId IconId => LogoThemedIcons.UnityLogo.Id;
        private readonly UnityHost myHost;
        private readonly BulbMenuComponent myBulbMenu;
        
        public AbstractUnityCodeInsightProvider(UnityHost host, BulbMenuComponent bulbMenu)
        {
            myHost = host;
            myBulbMenu = bulbMenu;
        }
        
        public void OnClick(CodeInsightsHighlighting highlighting)
        {
            var windowContextSource = new PopupWindowContextSource(lt => new HostTextControlPopupWindowContext(lt, highlighting.DeclaredElement.GetSolution().GetComponent<TextControlManager>().LastFocusedTextControl.Value).MarkAsOriginatedFromDataContext()/*new MousePositionPopupWindowContext(lt)*/);
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
               myHost.PerformModelAction(model => model.StartUnity.Fire(RdVoid.Instance));
           }
        }

        public abstract string ProviderId { get; }
        public abstract string DisplayName { get; }
        public abstract CodeLensAnchorKind DefaultAnchor { get; }
        
        public abstract ICollection<CodeLensRelativeOrdering> RelativeOrderings { get; }
    }
}