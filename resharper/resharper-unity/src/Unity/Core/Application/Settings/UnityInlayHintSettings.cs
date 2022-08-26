using JetBrains.Application.Settings;
using JetBrains.Application.Settings.WellKnownRootKeys;
using JetBrains.TextControl.DocumentMarkup.IntraTextAdornments;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings
{
    [SettingsKey(typeof(InlayHintsSettings), "Inlay hint settings for Unity related files")]
    public class UnityInlayHintSettings
    {
        [SettingsEntry(InlayHintsMode.Default, "Visibility mode of hints for GUID references in .asmdef files")]
        public InlayHintsMode ShowAsmDefGuidReferenceNames;

        [SettingsEntry(InlayHintsMode.Default, "Visibility mode of hints for package versions in .asmdef files")]
        public InlayHintsMode ShowAsmDefVersionDefinePackageVersions;
    }
}