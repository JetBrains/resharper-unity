using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.ReSharper.Feature.Services.InlayHints;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.AsmDef.ProjectModel;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.InlayHints
{
    [InlayHintContextActionProvider(typeof(AsmDefProjectFileType))]
    public class AsmDefInlayHintContextActionProvider : InlayHintContextActionsProvider<IAsmDefInlayHintContextActionHighlighting>
    {
        private readonly ISettingsStore mySettingsStore;

        public AsmDefInlayHintContextActionProvider(ISettingsStore settingsStore)
        {
            mySettingsStore = settingsStore;
        }

        public override IEnumerable<IntentionAction> GetPerHighlightingActions(
            IEnumerable<IInlayHintContextActionHighlighting> highlighting, IAnchor configureAnchor)
        {
            yield break;
        }

        public override IEnumerable<IntentionAction> GetCommonActions(
            IEnumerable<IInlayHintContextActionHighlighting> highlightings, IAnchor configureAnchor)
        {
            var highlighting = (IAsmDefInlayHintContextActionHighlighting)highlightings.First();
            return highlighting.BulbActionsProvider.CreateChangeVisibilityActions(mySettingsStore, highlighting,
                configureAnchor);
        }

        public override string GetOptionsPageId(IEnumerable<IInlayHintContextActionHighlighting> highlightings)
        {
            var highlighting = (IAsmDefInlayHintContextActionHighlighting)highlightings.First();
            return highlighting.BulbActionsProvider.GetOptionsPageId();
        }
    }
}