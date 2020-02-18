using JetBrains.Application.UI.Tooltips;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Caches.AssetHierarchy.Elements;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.TextControl.TextControlsManagement;

namespace JetBrains.ReSharper.Plugins.Unity.Rider
{
    [SolutionComponent]
    public class RiderUnityAssetOccurrenceNavigator : UnityAssetOccurrenceNavigator
    {
        public override bool Navigate(ISolution solution, IDeclaredElementPointer<IDeclaredElement> pointer, IHierarchyElement parent)
        {
            if (!solution.GetComponent<ConnectionTracker>().IsConnectionEstablished())
            {
                
                var textControl = solution.GetComponent<TextControlManager>().LastFocusedTextControl.Value;
                if (textControl == null)
                    return true;

                var tooltipManager = solution.GetComponent<ITooltipManager>();
                tooltipManager.Show("Start the Unity Editor to view changes in the Inspector", lifetime => textControl.PopupWindowContextFactory.CreatePopupWindowContext(lifetime));
                return true;
            }

            var findRequestCreator = solution.GetComponent<UnityEditorFindUsageResultCreator>();
            var declaredElement = pointer.FindDeclaredElement();
            if (declaredElement == null)
                return true;
            
            findRequestCreator.CreateRequestToUnity(declaredElement, parent, true);
            return false;
        }
    }
}