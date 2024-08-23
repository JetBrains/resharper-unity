using JetBrains.Annotations;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.Daemon;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings
{
    // TODO remove in 19.2 
    [ShellComponent(Instantiation.ContainerSyncPrimaryThread)]
    public class ConfigurableSeverityHacks
    {
        [NotNull] private static readonly Severity[] ourSeverities = {
            Severity.HINT,
            Severity.WARNING,
        };

        [NotNull] private static readonly string[] ourHighlightingIds = {
            PerformanceHighlightingAttributeIds.CAMERA_MAIN,
            PerformanceHighlightingAttributeIds.NULL_COMPARISON,
            PerformanceHighlightingAttributeIds.COSTLY_METHOD_INVOCATION,
            PerformanceHighlightingAttributeIds.INEFFICIENT_MULTIPLICATION_ORDER,
            PerformanceHighlightingAttributeIds.INEFFICIENT_MULTIDIMENSIONAL_ARRAYS_USAGE,
        };

        public ConfigurableSeverityHacks()
        {
            var severityIds = HighlightingAttributeIds.ValidHighlightingsForSeverity;
            lock (severityIds)
            {
                foreach (var severity in ourSeverities)
                {
                    if (!severityIds.TryGetValue(severity, out var collection)) continue;

                    foreach (var highlightingId in ourHighlightingIds)
                    {
                        if (!collection.Contains(highlightingId))
                        {
                            collection.Add(highlightingId);
                        }
                    }
                }
            }
        }
    }
}