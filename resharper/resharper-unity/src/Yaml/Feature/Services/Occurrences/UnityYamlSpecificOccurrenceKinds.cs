using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.Occurrences;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Feature.Services.Occurrences
{
    public static class UnityYamlSpecificOccurrenceKinds
    {
        [NotNull] public static readonly OccurrenceKind EventHandler =
            new OccurrenceKind("Unity event handler", OccurrenceKind.SemanticAxis, false);

        [NotNull] public static readonly OccurrenceKind ComponentUsage =
            new OccurrenceKind("Unity component usage", OccurrenceKind.SemanticAxis, false);
    }
}