using JetBrains.Application.InlayHints;
using JetBrains.Application.Settings;
using JetBrains.Application.Settings.WellKnownRootKeys;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings
{
    [SettingsKey(typeof(InlayHintsSettings), "Inlay hint settings for Unity related files")]
    public class UnityInlayHintSettings
    {
        [SettingsEntry(InlayHintsMode.Default, "Visibility mode of hints for GUID references in .asmdef files")]
        public InlayHintsMode ShowAsmDefGuidReferenceNames;
    }
}