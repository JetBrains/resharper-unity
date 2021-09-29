using System.Collections.Generic;
using JetBrains.Application.Settings;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.InlayHints;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.UI.Options;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.InlayHints
{
    public class AsmDefGuidReferenceInlayHintBulbActionsProvider : IInlayHintBulbActionsProvider
    {
        public IEnumerable<IntentionAction> CreateChangeVisibilityActions(ISettingsStore settingsStore,
                                                                          IHighlighting highlighting,
                                                                          IAnchor anchor)
        {
            return IntraTextAdornmentDataModelHelper.CreateChangeVisibilityActions(settingsStore,
                (UnityInlayHintSettings s) => s.ShowAsmDefGuidReferenceNames, anchor);
        }

        public IEnumerable<BulbMenuItem> CreateChangeVisibilityBulbMenuItems(ISettingsStore settingsStore,
                                                                             IHighlighting highlighting)
        {
            return IntraTextAdornmentDataModelHelper.CreateChangeVisibilityBulbMenuItems(settingsStore,
                (UnityInlayHintSettings s) => s.ShowAsmDefGuidReferenceNames, BulbMenuAnchors.FirstClassContextItems);
        }

        public string GetOptionsPageId() => UnityInlayHintsOptionsPage.PID;
    }
}