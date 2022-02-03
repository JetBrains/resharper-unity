using JetBrains.Annotations;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon.CodeInsights;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Psi;
using JetBrains.Rider.Model;
using JetBrains.TextControl.DocumentMarkup;
using Severity = JetBrains.ReSharper.Feature.Services.Daemon.Severity;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Common.CSharp.Daemon.CodeInsights
{
    [
        RegisterHighlighter(
            Id,
            GroupId = HighlighterGroupIds.HIDDEN,
            Layer = HighlighterLayer.SYNTAX + 1,
            EffectType = EffectType.NONE,
            NotRecyclable = false,
            TransmitUpdates = true
        )
    ]
    [StaticSeverityHighlighting(
        Severity.INFO,
        typeof(HighlightingGroupIds.CodeInsights),
        AttributeId = Id,
        OverlapResolve = OverlapResolveKind.NONE
    )]
    public class UnityInspectorCodeInsightsHighlighting : CodeInsightsHighlighting, IUnityIndicatorHighlighting, IHighlightingWithTestOutput
    {
        public new const string Id = "UnityInspectorCodeInsights";

        public readonly UnityCodeInsightFieldUsageProvider.UnityPresentationType UnityPresentationType;

        public UnityInspectorCodeInsightsHighlighting(DocumentRange range, [NotNull] string lenText, string tooltipText, [NotNull] string moreText,
                                             [NotNull] ICodeInsightsProvider provider, IDeclaredElement element,
                                             [CanBeNull] IconModel icon, UnityCodeInsightFieldUsageProvider.UnityPresentationType unityPresentationType)
            : base(range, lenText, tooltipText, moreText, provider, element, icon)
        {
            UnityPresentationType = unityPresentationType;
        }

        public string TestOutput => ((TextCodeLensEntry)Entry).Text
                                    + " | " + ((TextCodeLensEntry)Entry).LongPresentation
                                    + " | " + ((TextCodeLensEntry)Entry).Tooltip;
    }
}