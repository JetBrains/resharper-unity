using System.Collections.Generic;
using ICSharpCode.NRefactory;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Occurrences
{
    [SolutionComponent]
    public class UnityYamlOccurrenceKindProvider : IOccurrenceKindProvider
    {
        public ICollection<OccurrenceKind> GetOccurrenceKinds(IOccurrence occurrence)
        {
            var referenceOccurrence = occurrence as ReferenceOccurrence;
            if (referenceOccurrence == null)
                return EmptyList<OccurrenceKind>.Instance;

            var reference = referenceOccurrence.PrimaryReference;
            if (reference is UnityEventTargetReference)
                return new[] {UnityYamlSpecificOccurrenceKinds.EventHandler};
            return Util.EmptyList<OccurrenceKind>.Instance;
        }

        public IEnumerable<OccurrenceKind> GetAllPossibleOccurrenceKinds()
        {
            // What about invocation, name capture?
            return new[] {UnityYamlSpecificOccurrenceKinds.EventHandler};
        }
    }
}