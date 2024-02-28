using JetBrains.Application.Parts;
using JetBrains.Application.UI.Tooltips;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Protocol;
using JetBrains.ReSharper.Plugins.Unity.Rider.Resources;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetHierarchy.References;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Pointers;
using JetBrains.TextControl.CodeWithMe;
using JetBrains.TextControl.TextControlsManagement;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.Yaml.Feature.Services.Navigation
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class RiderUnityAssetOccurrenceNavigator : UnityAssetOccurrenceNavigator
    {
        public override bool Navigate(ISolution solution, IDeclaredElementPointer<IDeclaredElement> pointer, LocalReference location)
        {
            if (!solution.GetComponent<BackendUnityHost>().IsConnectionEstablished())
            {
                var textControl = solution.GetComponent<TextControlManager>().LastFocusedTextControlPerClient
                    .ForCurrentClient();
                if (textControl == null)
                    return true;

                var tooltipManager = solution.GetComponent<ITooltipManager>();
                tooltipManager.Show(Strings.RiderUnityAssetOccurrenceNavigator_Navigate_Start_the_Unity_Editor_to_view_results, textControl.PopupWindowContextFactory.ForCaret());
                return true;
            }

            var findRequestCreator = solution.GetComponent<UnityEditorFindUsageResultCreator>();
            var declaredElement = pointer.FindDeclaredElement();
            if (declaredElement == null)
                return true;

            findRequestCreator.CreateRequestToUnity(declaredElement, location);
            return false;
        }
    }
}