using JetBrains.Application.Parts;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;
using JetBrains.UI.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.Occurrences
{
    [SolutionComponent(Instantiation.DemandAnyThreadSafe)]
    public class AsmDefSpecificOccurrenceKindIconProvider : IOccurrenceKindIconProvider
    {
        public IconId GetImageId(OccurrenceKind occurrenceKind, IOccurrence occurrence)
        {
            if (occurrenceKind == AsmDefOccurrenceKindProvider.AssemblyDefinitionReference)
                return UnityFileTypeThemedIcons.UsageAsmdef.Id;
            return null;
        }

        public int GetPriority(OccurrenceKind occurrenceKind)
        {
            return 0;
        }
    }
}