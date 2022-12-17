using JetBrains.Application.Settings;
using JetBrains.Application.Settings.WellKnownRootKeys;
using JetBrains.TextControl.DocumentMarkup.IntraTextAdornments;
using JetBrains.ReSharper.Plugins.Unity.Resources;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings
{
    [SettingsKey(typeof(InlayHintsSettings), DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnityInlayHintSettings_t_Inlay_hint_settings_for_Unity_related_files))]
    public class UnityInlayHintSettings
    {
        [SettingsEntry(InlayHintsMode.Default, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnityInlayHintSettings_t_Visibility_mode_of_hints_for_GUID_references_in__asmdef_files))]
        public InlayHintsMode ShowAsmDefGuidReferenceNames;

        [SettingsEntry(InlayHintsMode.Default, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnityInlayHintSettings_t_Visibility_mode_of_hints_for_package_versions_in__asmdef_files))]
        public InlayHintsMode ShowAsmDefVersionDefinePackageVersions;
    }
}