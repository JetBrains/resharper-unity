using System.Collections.Generic;
using JetBrains.Application.UI.Controls.GotoByName;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.Collections.Viewable;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Daemon.CodeInsights;
using JetBrains.ReSharper.Host.Features.TextControls;
using JetBrains.ReSharper.Plugins.Unity.ProjectModel;
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

        private readonly UnitySolutionTracker myUnitySolutionTracker;
        private readonly UnityHost myHost;
        private readonly BulbMenuComponent myBulbMenu;

        protected AbstractUnityCodeInsightProvider(UnitySolutionTracker unitySolutionTracker, UnityHost host, BulbMenuComponent bulbMenu)
        {
            myUnitySolutionTracker = unitySolutionTracker;
            myHost = host;
            myBulbMenu = bulbMenu;
        }
        
        public void OnClick(CodeInsightsHighlighting highlighting, ISolution solution)
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
               myHost.PerformModelAction(model => model.StartUnity());
           }
        }

        public virtual IconId IconId => InsightUnityIcons.InsightUnity.Id;

        public bool IsAvailableIn(ISolution solution) => myUnitySolutionTracker.IsUnityProject.HasTrueValue();


        public abstract string ProviderId { get; }
        public abstract string DisplayName { get; }
        public abstract CodeLensAnchorKind DefaultAnchor { get; }
        public abstract ICollection<CodeLensRelativeOrdering> RelativeOrderings { get; }
    }
}