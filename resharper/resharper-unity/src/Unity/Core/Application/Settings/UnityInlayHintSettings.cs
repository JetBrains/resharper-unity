using JetBrains.Application.Settings;
using JetBrains.Application.Settings.WellKnownRootKeys;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.TextControl.DocumentMarkup.Adornments;
using JetBrains.TextControl.DocumentMarkup.IntraTextAdornments;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings
{
    [SettingsKey(typeof(InlayHintsSettings), DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnityInlayHintSettings_t_Inlay_hint_settings_for_Unity_related_files))]
    public class UnityInlayHintSettings
    {
        [SettingsEntry(PushToHintMode.Default, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnityInlayHintSettings_t_Visibility_mode_of_hints_for_GUID_references_in__asmdef_files))]
        public PushToHintMode ShowAsmDefGuidReferenceNames;

        [SettingsEntry(PushToHintMode.Default, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnityInlayHintSettings_t_Visibility_mode_of_hints_for_package_versions_in__asmdef_files))]
        public PushToHintMode ShowAsmDefVersionDefinePackageVersions;
        
        [SettingsEntry(PushToHintMode.Default, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnityInlayHintsOptionsPage_t_UnityInlayHintsOptionsPage_UnityObjectNullComparisonHint))]
        public PushToHintMode UnityObjectNullComparisonHint;
    }
}