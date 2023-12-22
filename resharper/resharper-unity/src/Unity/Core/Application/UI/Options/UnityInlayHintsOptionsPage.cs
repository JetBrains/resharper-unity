using JetBrains.Application.UI.Icons.FeaturesIntellisenseThemedIcons;
using JetBrains.Application.UI.Options;
using JetBrains.Application.UI.Options.OptionsDialog;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.InlayHints;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Application.UI.Options
{
    [OptionsPage(PID, "Unity",
        typeof(FeaturesIntellisenseThemedIcons.ParameterInfoPage),
        ParentId = InlayHintsOptionsPage.PID,
        NestingType = OptionPageNestingType.Child,
        Sequence = 7)]
    public class UnityInlayHintsOptionsPage : InlayHintsOptionPageBase
    {
        public const string PID = "UnityInlayHintsOptions";

        public UnityInlayHintsOptionsPage(Lifetime lifetime, OptionsPageContext optionsPageContext,
                                          OptionsSettingsSmartContext optionsSettingsSmartContext,
                                          bool wrapInScrollablePanel = false)
            : base(lifetime, optionsPageContext, optionsSettingsSmartContext, wrapInScrollablePanel)
        {
            AddVisibilityHelpText();

            AddHeader(Strings.UnityInlayHintsOptionsPage_UnityInlayHintsOptionsPage_Assembly_Definition_file_GUID_references);
            AddVisibilityOption((UnityInlayHintSettings s) => s.ShowAsmDefGuidReferenceNames);

            AddHeader(Strings.UnityInlayHintsOptionsPage_UnityInlayHintsOptionsPage_Assembly_Definition_file_package_versions);
            AddVisibilityOption((UnityInlayHintSettings s) => s.ShowAsmDefVersionDefinePackageVersions);
        }
    }
}