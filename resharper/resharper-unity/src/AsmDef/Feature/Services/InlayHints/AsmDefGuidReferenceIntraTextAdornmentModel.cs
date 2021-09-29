using System.Collections.Generic;
using JetBrains.Application.InlayHints;
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
using JetBrains.TextControl.DocumentMarkup;
using JetBrains.UI.Icons;
using JetBrains.UI.RichText;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace JetBrains.ReSharper.Plugins.Unity.AsmDef.Feature.Services.InlayHints
{
    public class AsmDefGuidReferenceIntraTextAdornmentModel : IIntraTextAdornmentDataModel
    {
        private readonly IAsmDefInlayHintHighlighting myHighlighting;
        private readonly ISolution mySolution;
        private readonly ISettingsStore mySettingsStore;
        private IList<BulbMenuItem> myContextMenuItems;

        public AsmDefGuidReferenceIntraTextAdornmentModel(IAsmDefInlayHintHighlighting highlighting,
                                                          ISolution solution,
                                                          ISettingsStore settingsStore)
        {
            myHighlighting = highlighting;
            mySolution = solution;
            mySettingsStore = settingsStore;

            ContextMenuTitle = new PresentableItem(FeaturesIntellisenseThemedIcons.ParameterInfoPage.Id,
                highlighting.ContextMenuTitle);
        }

        public void ExecuteNavigation(PopupWindowContextSource popupWindowContextSource)
        {
        }

        public RichText Text => myHighlighting.Text;
        public InlayHintsMode InlayHintsMode => myHighlighting.Mode;
        public bool IsPreceding => false;
        public int Order => 0;

        // Context menu appears to be ReSharper only. Rider doesn't show any context menus for inlay hints
        public bool HasContextMenu => true;
        public IPresentableItem ContextMenuTitle { get; }
        public IEnumerable<BulbMenuItem> ContextMenuItems => myContextMenuItems ??= BuildContextMenuItems();

        public bool IsNavigable => false;
        public TextRange? SelectionRange => null;
        public IconId IconId => null;

        private IList<BulbMenuItem> BuildContextMenuItems()
        {
            var visibilityItems = IntraTextAdornmentDataModelHelper.CreateChangeVisibilityBulbMenuItems(mySettingsStore,
                (UnityInlayHintSettings s) => s.ShowAsmDefGuidReferenceNames, BulbMenuAnchors.SecondClassContextItems);
            return new List<BulbMenuItem>(visibilityItems)
            {
                IntraTextAdornmentDataModelHelper.CreateTurnOffAllInlayHintsBulbMenuItem(mySettingsStore),
                new(new ExecutableItem(() =>
                    {
                        var optionsDialogOwner = mySolution.TryGetComponent<IOptionsDialogViewOwner>();
                        if (optionsDialogOwner != null)
                            Logger.Catch(() => optionsDialogOwner.Show(page: UnityInlayHintsOptionsPage.PID));
                    }),
                    new RichText("Configure..."),
                    null,
                    BulbMenuAnchors.SecondClassContextItems)
            };
        }
    }
}