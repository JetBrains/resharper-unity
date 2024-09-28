using System.Collections.Generic;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.Daemon;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings
{
    [ShellComponent(Instantiation.DemandAnyThreadSafe)]
    public sealed class UnityCustomSeverityPresentationsProvider : IHighlightingCustomPresentationsForSeverityProvider
    {
        public IEnumerable<string> GetAttributeIdsForSeverity(Severity severity)
        {
            if (severity is Severity.HINT or Severity.WARNING)
            {
                return [
                    PerformanceHighlightingAttributeIds.CAMERA_MAIN,
                    PerformanceHighlightingAttributeIds.NULL_COMPARISON,
                    PerformanceHighlightingAttributeIds.COSTLY_METHOD_INVOCATION,
                    PerformanceHighlightingAttributeIds.INEFFICIENT_MULTIPLICATION_ORDER,
                    PerformanceHighlightingAttributeIds.INEFFICIENT_MULTIDIMENSIONAL_ARRAYS_USAGE
                ];
            }

            return [];
        }
    }
}