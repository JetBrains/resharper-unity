#nullable enable

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using JetBrains.Application.Settings;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.ReSharper.Feature.Services.Daemon;
using JetBrains.ReSharper.Feature.Services.InlayHints;
using JetBrains.ReSharper.Feature.Services.Intentions;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.UI.Options;
using JetBrains.TextControl.DocumentMarkup.IntraTextAdornments;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.InlayHints
{
    public class SimpleInlayHintBulbActionsProvider : IInlayHintBulbActionsProvider
    {
        private readonly Expression<Func<UnityInlayHintSettings, InlayHintsMode>> myOption;

        protected SimpleInlayHintBulbActionsProvider(Expression<Func<UnityInlayHintSettings, InlayHintsMode>> option)
        {
            myOption = option;
        }

        public IEnumerable<IntentionAction> CreateChangeVisibilityActions(ISettingsStore settingsStore,
                                                                          IHighlighting highlighting,
                                                                          IAnchor anchor)
        {
            return IntraTextAdornmentDataModelHelper.CreateChangeVisibilityActions(settingsStore, myOption, anchor);
        }

        public IEnumerable<BulbMenuItem> CreateChangeVisibilityBulbMenuItems(ISettingsStore settingsStore,
                                                                             IHighlighting highlighting)
        {
            return IntraTextAdornmentDataModelHelper.CreateChangeVisibilityBulbMenuItems(settingsStore, myOption,
                BulbMenuAnchors.FirstClassContextItems);
        }

        public string GetOptionsPageId() => UnityInlayHintsOptionsPage.PID;
    }
}