using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Occurrences
{
    [SolutionComponent]
    public class UnityYamlOccurrenceKindProvider : IOccurrenceKindProvider
    {
        public ICollection<OccurrenceKind> GetOccurrenceKinds(IOccurrence occurrence)
        {
            var referenceOccurrence = occurrence as ReferenceOccurrence;
            var reference = referenceOccurrence?.PrimaryReference;
            if (reference is UnityEventTargetReference)
                return new[] {UnityYamlSpecificOccurrenceKinds.EventHandler};
            if (reference is MonoScriptReference)
                return new[] {UnityYamlSpecificOccurrenceKinds.ComponentUsage};
            return EmptyList<OccurrenceKind>.Instance;
        }

        public IEnumerable<OccurrenceKind> GetAllPossibleOccurrenceKinds()
        {
            // What about invocation, name capture?
            return new[]
            {
                UnityYamlSpecificOccurrenceKinds.EventHandler,
                UnityYamlSpecificOccurrenceKinds.ComponentUsage
            };
        }
    }
}