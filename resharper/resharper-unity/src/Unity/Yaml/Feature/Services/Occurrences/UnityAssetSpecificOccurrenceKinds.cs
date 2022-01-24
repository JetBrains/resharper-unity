using JetBrains.ReSharper.Feature.Services.Occurrences;

#nullable enable

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Occurrences
{
    public static class UnityAssetSpecificOccurrenceKinds
    {
        public static readonly OccurrenceKind EventHandler = OccurrenceKind.CreateSemantic("Unity event handler");
        public static readonly OccurrenceKind ComponentUsage = OccurrenceKind.CreateSemantic("Unity component usage");
        public static readonly OccurrenceKind InspectorUsage = OccurrenceKind.CreateSemantic("Inspector values");
    }
}