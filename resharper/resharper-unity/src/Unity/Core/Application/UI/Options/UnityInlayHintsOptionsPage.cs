using JetBrains.Application.UI.Options;
using JetBrains.Application.UI.Options.OptionsDialog;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Feature.Services.InlayHints;
using JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Plugins.Unity.Resources.Icons;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Application.UI.Options
{
    [OptionsPage(PID, "Unity",
        typeof(LogoIcons.Unity),
        ParentId = InlayHintsOptionsPage.PID,
        NestingType = OptionPageNestingType.Child,
        Sequence = 7)]
    public class UnityInlayHintsOptionsPage : InlayHintsOptionPageBase
    {
        public const string PID = "UnityInlayHintsOptions";

        public UnityInlayHintsOptionsPage(Lifetime lifetime, OptionsPageContext optionsPageContext,
                                          OptionsSettingsSmartContext optionsSettingsSmartContext)
            : base(lifetime, optionsPageContext, optionsSettingsSmartContext)
        {
            AddVisibilityHelpText();

            AddHeader(Strings.UnityInlayHintsOptionsPage_UnityInlayHintsOptionsPage_Assembly_Definition_file_GUID_references);
            AddVisibilityOption((UnityInlayHintSettings s) => s.ShowAsmDefGuidReferenceNames);

            AddHeader(Strings.UnityInlayHintsOptionsPage_UnityInlayHintsOptionsPage_Assembly_Definition_file_package_versions);
            AddVisibilityOption((UnityInlayHintSettings s) => s.ShowAsmDefVersionDefinePackageVersions);
            
            AddHeader(Strings.UnityInlayHintsOptionsPage_UnityInlayHintsOptionsPage_UnityObjectNullComparisonHint);
            AddVisibilityOption((UnityInlayHintSettings s) => s.UnityObjectNullComparisonHint);

        }
    }
}