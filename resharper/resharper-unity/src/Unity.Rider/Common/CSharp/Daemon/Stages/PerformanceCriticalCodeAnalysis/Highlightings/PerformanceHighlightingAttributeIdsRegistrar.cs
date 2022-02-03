using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings;
using JetBrains.TextControl.DocumentMarkup;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings
{
    // VisualStudio doesn't support EffectType.LINE_MARKER, so we need separate registrations
    [RegisterHighlighter(PerformanceHighlightingAttributeIds.PERFORMANCE_CRITICAL_METHOD_HIGHLIGHTER,
        GroupId = UnityHighlightingAttributeIds.GROUP_ID,
        BackgroundColor = "#ff7526",
        DarkBackgroundColor = "#ff7526",
        EffectType = EffectType.LINE_MARKER,
        ForegroundColor = "#ff7526",
        DarkForegroundColor = "#ff7526",
        Layer = HighlighterLayer.ADDITIONAL_SYNTAX,
        TransmitUpdates = true)]
    public class PerformanceHighlightingAttributeIdsRegistrar
    {
    }
}