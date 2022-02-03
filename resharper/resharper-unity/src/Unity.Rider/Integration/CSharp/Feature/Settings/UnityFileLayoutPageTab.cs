using JetBrains.Application.UI.Options;
using JetBrains.Application.UI.Options.OptionsDialog;
using JetBrains.IDE.UI.Extensions;
using JetBrains.IDE.UI.Extensions.Properties;
using JetBrains.Lifetimes;
using JetBrains.Rider.Backend.Features.Dialog;
using JetBrains.Rider.Backend.Features.Settings.OptionsPage.CSharpFileLayout;
using JetBrains.Rider.Model;
using JetBrains.Rider.Model.UIAutomation;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration.CSharp.Feature.Settings
{
    [OptionsComponent]
    public class UnityFileLayoutPageTab : IFileLayoutPageTab
    {
        private readonly RiderDialogHost myDialogHost;
        private readonly RdLanguage myFileLayoutLanguage = new RdLanguage("XML");

        private const string DummyFileName = "Dummy.filelayout";

        public UnityFileLayoutPageTab(RiderDialogHost dialogHost)
        {
            myDialogHost = dialogHost;
        }

        public string Title => "Unity";

        public BeControl Create(Lifetime lifetime, OptionsPageContext optionsPageContext,
                                OptionsSettingsSmartContext optionsSettingsSmartContext)
        {
            var fileLayoutSettings = new AdditionalFileLayoutSettingsHelper(lifetime, optionsSettingsSmartContext, myDialogHost);
            var textControl = BeControls.GetLanguageTextControl(fileLayoutSettings.Text, lifetime, false, myFileLayoutLanguage, DummyFileName, true);
            var toolbar = BeControls.GetToolbar(textControl);

            var emptyPatternItem = BeControls.GetButton("Empty", lifetime, () => fileLayoutSettings.LoadDefaultPattern(DefaultPatternKind.Empty));
            var defaultPatternWithoutRegionsItem = BeControls.GetButton("Default", lifetime, () => fileLayoutSettings.LoadDefaultPattern(DefaultPatternKind.WithoutRegions));
            var defaultPatternWithRegionsItem = BeControls.GetButton("Default with regions", lifetime, () => fileLayoutSettings.LoadDefaultPattern(DefaultPatternKind.WithRegions));
            toolbar.AddItem("Load patterns:".GetBeLabel());
            toolbar.AddItem(emptyPatternItem);
            toolbar.AddItem(defaultPatternWithoutRegionsItem);
            toolbar.AddItem(defaultPatternWithRegionsItem);

            var grid = BeControls.GetGrid();
            grid.AddElement(toolbar, BeSizingType.Fill);

            var margin = BeMargins.Create(5, 1, 5, 1);
            return grid.WithMargin(margin);
        }
    }
}