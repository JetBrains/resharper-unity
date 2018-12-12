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

[assembly: RegisterHighlighter(UnityCodeInsightsHighlighting.Id, 
    EffectType = EffectType.NONE, Layer = HighlighterLayer.SYNTAX + 1, NotRecyclable = true, GroupId = HighlighterGroupIds.HIDDEN)]


namespace JetBrains.ReSharper.Plugins.Unity.Rider.CodeInsights
{
    [StaticSeverityHighlighting(Severity.INFO, HighlightingGroupIds.CodeInsightsGroup, 
        AttributeId = Id, OverlapResolve = OverlapResolveKind.NONE 
    )]
    public class UnityCodeInsightsHighlighting : CodeInsightsHighlighting, IUnityHighlighting
    {
        public const string Id = "UnityCodeInsights";
        public List<BulbMenuItem> MenuItems { get; }

        public UnityCodeInsightsHighlighting(DocumentRange range, [NotNull] string lenText, [NotNull] string moreText, [NotNull] ICodeInsightsProvider provider, IDeclaredElement elt, [CanBeNull] Icon icon, IEnumerable<BulbMenuItem> menuItems, List<CodeLensEntryExtraActionModel> extraActions = null)
            : base(range, lenText, moreText, provider, elt, icon, extraActions)
        {
            MenuItems = menuItems.ToList();
        }
    }
}