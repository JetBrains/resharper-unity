#nullable enable

using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using JetBrains.Application.Settings;
using JetBrains.Application.UI.Controls.BulbMenu.Anchors;
using JetBrains.Application.UI.Controls.BulbMenu.Items;
using JetBrains.Application.UI.Controls.Utils;
using JetBrains.Application.UI.Icons.FeaturesIntellisenseThemedIcons;
using JetBrains.Application.UI.Options.OptionsDialog;
using JetBrains.Application.UI.PopupLayout;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.InlayHints;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.UI.Options;
using JetBrains.TextControl.DocumentMarkup.IntraTextAdornments;
using JetBrains.UI.RichText;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.InlayHints
{
    public class AsmDefIntraTextAdornmentModel : IIntraTextAdornmentDataModel
    {
        private readonly IAsmDefInlayHintHighlighting myHighlighting;
        private readonly Expression<Func<UnityInlayHintSettings, InlayHintsMode>> myOption;
        private readonly ISolution mySolution;
        private readonly ISettingsStore mySettingsStore;
        private IList<BulbMenuItem>? myContextMenuItems;

        private readonly IntraTextAdornmentData myData;

        public AsmDefIntraTextAdornmentModel(IAsmDefInlayHintHighlighting highlighting,
                                             Expression<Func<UnityInlayHintSettings, InlayHintsMode>> option,
                                             ISolution solution,
                                             ISettingsStore settingsStore)
        {
            myHighlighting = highlighting;
            myOption = option;
            mySolution = solution;
            mySettingsStore = settingsStore;

            ContextMenuTitle = new PresentableItem(FeaturesIntellisenseThemedIcons.ParameterInfoPage.Id,
                highlighting.ContextMenuTitle);
            
            myData = IntraTextAdornmentData.New.
                WithText(myHighlighting.Text).
                WithMode(myHighlighting.Mode).
                WithFlags(IntraTextAdornmentFlags.HasContextMenu); // Context menu appears to be ReSharper only. Rider doesn't show any context menus for inlay hints
        }

        public void ExecuteNavigation(PopupWindowContextSource popupWindowContextSource)
        {
        }
        
        public IPresentableItem ContextMenuTitle { get; }
        public IEnumerable<BulbMenuItem> ContextMenuItems => myContextMenuItems ??= BuildContextMenuItems();

        public TextRange? SelectionRange => null;

        private IList<BulbMenuItem> BuildContextMenuItems()
        {
            var visibilityItems = IntraTextAdornmentDataModelHelper.CreateChangeVisibilityBulbMenuItems(mySettingsStore,
                myOption, BulbMenuAnchors.SecondClassContextItems);
            return new List<BulbMenuItem>(visibilityItems)
            {
                IntraTextAdornmentDataModelHelper.CreateTurnOffAllInlayHintsBulbMenuItem(mySettingsStore),
                new(new ExecutableItem(() =>
                    {
                        var optionsDialogOwner = mySolution.TryGetComponent<IOptionsDialogViewOwner>();
                        if (optionsDialogOwner != null)
                            Logger.Catch(() => optionsDialogOwner.Show(page: UnityInlayHintsOptionsPage.PID));
                    }),
                    new RichText("Configure…"),
                    null,
                    BulbMenuAnchors.SecondClassContextItems)
            };
        }

        /// <inheritdoc />
        IntraTextAdornmentData IIntraTextAdornmentDataModel.Data => myData;
    }
}