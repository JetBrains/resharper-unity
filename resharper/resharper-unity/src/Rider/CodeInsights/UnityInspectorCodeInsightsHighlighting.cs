using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Daemon.CodeInsights;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Plugins.Unity.CSharp.Daemon.Stages.Highlightings;
using JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights;
using JetBrains.ReSharper.Psi;
using JetBrains.Rider.Model;
using JetBrains.TextControl.DocumentMarkup;
using Severity = JetBrains.ReSharper.Feature.Services.Daemon.Severity;

[assembly: RegisterHighlighter(UnityInspectorCodeInsightsHighlighting.Id, EffectType = EffectType.NONE,
    Layer = HighlighterLayer.SYNTAX + 1, NotRecyclable = true, GroupId = HighlighterGroupIds.HIDDEN, TransmitUpdates = true)]


namespace JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights
{
    [StaticSeverityHighlighting(Severity.INFO, HighlightingGroupIds.CodeInsightsGroup, AttributeId = Id,
        OverlapResolve = OverlapResolveKind.NONE)]
    public class UnityInspectorCodeInsightsHighlighting : CodeInsightsHighlighting, IUnityHighlighting
    {
        public const string Id = "UnityInspectorCodeInsights";

        public readonly UnityCodeInsightFieldUsageProvider.UnityPresentationType UnityPresentationType;

        public UnityInspectorCodeInsightsHighlighting(DocumentRange range, [NotNull] string lenText, string tooltipText, [NotNull] string moreText,
                                             [NotNull] ICodeInsightsProvider provider, IDeclaredElement element,
                                             [CanBeNull] IconModel icon, UnityCodeInsightFieldUsageProvider.UnityPresentationType unityPresentationType)
            : base(range, lenText, tooltipText, moreText, provider, element, icon)
        {
            UnityPresentationType = unityPresentationType;
        }
    }
}