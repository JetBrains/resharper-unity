using System.Collections.Generic;
using JetBrains.ReSharper.Feature.Services.Occurrences; 

namespace JetBrains.ReSharper.Plugins.Unity.AsmDefCommon.Feature.Services.Occurrences
{
    // Group usages of the name element by "Assembly definition reference"
    public abstract class AsmDefOccurrenceKindProviderBase<TReference> : IOccurrenceKindProvider
    {
        public static readonly OccurrenceKind AssemblyDefinitionReference = new OccurrenceKind("Assembly definition reference", OccurrenceKind.SemanticAxis);

        public ICollection<OccurrenceKind> GetOccurrenceKinds(IOccurrence occurrence)
        {
            if (occurrence is IAsmDefNameOccurence)
                return new[] {AssemblyDefinitionReference};
            
            var referenceOccurrence = occurrence as ReferenceOccurrence;
            var reference = referenceOccurrence?.PrimaryReference;
            if (reference is TReference)
                return new[] {AssemblyDefinitionReference};

            return null;
        }

        public IEnumerable<OccurrenceKind> GetAllPossibleOccurrenceKinds()
        {
            return new[] {AssemblyDefinitionReference};
        }
    }
}