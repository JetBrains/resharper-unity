using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDefNew.Feature.Services.Occurrences
{
    // Group usages of the name element by "Assembly definition reference"
    [SolutionComponent]
    public class AsmDefOccurrenceKindProvider : IOccurrenceKindProvider
    {
        public static readonly OccurrenceKind AssemblyDefinitionReference = new OccurrenceKind("Assembly definition reference", OccurrenceKind.SemanticAxis);

        public ICollection<OccurrenceKind> GetOccurrenceKinds(IOccurrence occurrence)
        {
            if (occurrence is AsmDefNameOccurrence)
                return new[] {AssemblyDefinitionReference};

            return null;
        }

        public IEnumerable<OccurrenceKind> GetAllPossibleOccurrenceKinds()
        {
            return new[] {AssemblyDefinitionReference};
        }
    }
}