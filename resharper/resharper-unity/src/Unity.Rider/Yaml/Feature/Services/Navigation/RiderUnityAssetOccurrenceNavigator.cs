using JetBrains.Application.UI.PopupLayout;
using JetBrains.Application.UI.Tooltips;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.TextControl.TextControlsManagement;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Yaml.Feature.Services.Navigation
{
    [SolutionComponent]
    public class RiderUnityAssetOccurrenceNavigator : UnityAssetOccurrenceNavigator
    {
        public override bool Navigate(ISolution solution, IDeclaredElementPointer<IDeclaredElement> pointer, LocalReference location)
        {
            if (!solution.GetComponent<BackendUnityHost>().IsConnectionEstablished())
            {
                var textControl = solution.GetComponent<TextControlManager>().LastFocusedTextControl.Value;
                if (textControl == null)
                    return true;

                var tooltipManager = solution.GetComponent<ITooltipManager>();
                tooltipManager.Show("Start the Unity Editor to view results",
                    new PopupWindowContextSource(lifetime =>
                        textControl.PopupWindowContextFactory.CreatePopupWindowContext(lifetime)));
                return true;
            }

            var findRequestCreator = solution.GetComponent<UnityEditorFindUsageResultCreator>();
            var declaredElement = pointer.FindDeclaredElement();
            if (declaredElement == null)
                return true;

            findRequestCreator.CreateRequestToUnity(declaredElement, location, true);
            return false;
        }
    }
}