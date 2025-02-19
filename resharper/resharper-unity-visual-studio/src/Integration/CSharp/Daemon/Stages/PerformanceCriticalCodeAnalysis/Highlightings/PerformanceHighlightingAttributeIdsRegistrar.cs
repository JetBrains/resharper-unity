using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings;
using JetBrains.TextControl.DocumentMarkup;
using JetBrains.TextControl.DocumentMarkup.VisualStudio;

namespace JetBrains.ReSharper.Plugins.Unity.VisualStudio.Integration.CSharp.Daemon.Stages.PerformanceCriticalCodeAnalysis.Highlightings
{
    // VisualStudio doesn't support EffectType.LINE_MARKER, so we need a separate registration for SOLID_UNDERLINE
    [RegisterHighlighter(
        PerformanceHighlightingAttributeIds.PERFORMANCE_CRITICAL_METHOD_HIGHLIGHTER,
        GroupId = UnityHighlightingAttributeIds.GROUP_ID,
        ForegroundColor = "#ff7526",
        DarkForegroundColor = "#ff7526",
        EffectType = EffectType.SOLID_UNDERLINE,
        VsGenerateClassificationDefinition = VsGenerateDefinition.VisibleClassification,
        Layer = HighlighterLayer.WARNING + 1)]
    public class PerformanceHighlightingAttributeIdsRegistrar
    {
    }
}