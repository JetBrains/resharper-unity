using JetBrains.Application.UI.Options;
using JetBrains.Application.UI.Options.OptionsDialog;
using JetBrains.IDE.UI.Extensions;
using JetBrains.IDE.UI.Extensions.Properties;
using JetBrains.IDE.UI.Options;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.Resources;
using JetBrains.ReSharper.Host.Features.Dialog;
using JetBrains.ReSharper.Host.Features.Settings.OptionsPage.CSharpFileLayout;
using JetBrains.ReSharper.Plugins.Unity.Rider;
using JetBrains.Rider.Model;
using JetBrains.Rider.Model.UIAutomation;

namespace JetBrains.ReSharper.Plugins.Unity.CSharp.Psi.CodeStyle
{
    [OptionsPage(PID, "Additional C# File Layout",
        typeof(FeaturesEnvironmentOptionsThemedIcons.TypeMembersLayout),
        ParentId = UnityOptionsPage.PID
    )]
    public class AdditionalFileLayoutOptionsPage : BeSimpleOptionsPage
    {
        private const string PID = "UnityAdditionalFileLayout";

        private readonly RdLanguage myFileLayoutLanguage = new RdLanguage("XML");
        private const string DummyFileName = "Dummy.filelayout";

        public AdditionalFileLayoutOptionsPage(Lifetime lifetime, OptionsPageContext optionsPageContext,
                                               OptionsSettingsSmartContext optionsSettingsSmartContext,
                                               RiderDialogHost dialogHost)
            : base(lifetime, optionsPageContext, optionsSettingsSmartContext)
        {
            var fileLayoutSettings = new AdditionalFileLayoutSettingsHelper(lifetime, optionsSettingsSmartContext, dialogHost);
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
            AddControl(grid.WithMargin(margin), true);

            AddKeyword("File Layout");
        }
    }
}