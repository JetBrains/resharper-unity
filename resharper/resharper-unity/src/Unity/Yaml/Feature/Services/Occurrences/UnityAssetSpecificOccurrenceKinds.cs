using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Plugins.Unity.Resources;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Occurrences
{
    public static class UnityAssetSpecificOccurrenceKinds
    {
        public static readonly OccurrenceKind EventHandler = OccurrenceKind.CreateSemantic(Strings.UnityAssetSpecificOccurrenceKinds_EventHandler_Unity_event_handler);
        public static readonly OccurrenceKind ComponentUsage = OccurrenceKind.CreateSemantic(Strings.UnityAssetSpecificOccurrenceKinds_ComponentUsage_Unity_component_usage);
        public static readonly OccurrenceKind InspectorUsage = OccurrenceKind.CreateSemantic(Strings.UnityAssetSpecificOccurrenceKinds_InspectorUsage_Inspector_values);
    }
}