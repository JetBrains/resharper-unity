using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Occurrences
{
    [SolutionComponent]
    public class UnityYamlSpecificOccurrenceKindIconProvider : IOccurrenceKindIconProvider
    {
        public IconId GetImageId(OccurrenceKind declaredElement)
        {
            // TODO: Better icon?
            if (declaredElement == UnityYamlSpecificOccurrenceKinds.EventHandler)
                return ServicesNavigationThemedIcons.UsageEventDeclaration.Id;
            return null;
        }

        public int GetPriority(OccurrenceKind occurrenceKind)
        {
            if (occurrenceKind == UnityYamlSpecificOccurrenceKinds.EventHandler)
                return -10;
            return 0;
        }
    }
}