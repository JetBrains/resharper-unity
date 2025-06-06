using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Occurrences
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class UnityYamlSpecificOccurrenceKindIconProvider : IOccurrenceKindIconProvider
    {
        public IconId GetImageId(OccurrenceKind declaredElement, IOccurrence occurrence)
        {
            // TODO: Better icon?
            if (declaredElement == UnityAssetSpecificOccurrenceKinds.EventHandler)
                return ServicesNavigationThemedIcons.UsageEventDeclaration.Id;
            return null;
        }

        public int GetPriority(OccurrenceKind occurrenceKind)
        {
            if (occurrenceKind == UnityAssetSpecificOccurrenceKinds.EventHandler ||
                occurrenceKind == UnityAssetSpecificOccurrenceKinds.ComponentUsage ||
                occurrenceKind == UnityAssetSpecificOccurrenceKinds.InspectorUsage)
                return -10;
            return 0;
        }
    }
}