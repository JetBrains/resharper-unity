using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Navigation;
using JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.Resolve;
using JetBrains.Util;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Occurrences
{
    [SolutionComponent]
    public class UnityEditorOccurrenceKindProvider : IOccurrenceKindProvider
    {
        public ICollection<OccurrenceKind> GetOccurrenceKinds(IOccurrence occurrence)
        {
            if (occurrence is UnityEventHandlerOccurrence)
                return new[] {UnityAssetSpecificOccurrenceKinds.EventHandler};
            if (occurrence is UnityScriptsOccurrence)
                return new[] {UnityAssetSpecificOccurrenceKinds.ComponentUsage};
            if (occurrence is UnityInspectorValuesOccurrence)
                return new[] {UnityAssetSpecificOccurrenceKinds.InspectorUsage};
            if (occurrence is UnityEventSubscriptionOccurrence)
                return new[] {UnityAssetSpecificOccurrenceKinds.EventHandler};
            return EmptyList<OccurrenceKind>.Instance;
        }

        public IEnumerable<OccurrenceKind> GetAllPossibleOccurrenceKinds()
        {
            // What about invocation, name capture?
            return new[]
            {
                UnityAssetSpecificOccurrenceKinds.EventHandler,
                UnityAssetSpecificOccurrenceKinds.ComponentUsage,
                UnityAssetSpecificOccurrenceKinds.InspectorUsage
            };
        }
    }
}