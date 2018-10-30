// ReSharper disable InconsistentNaming
using JetBrains.Application.Settings;
using JetBrains.ReSharper.Resources.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.Settings
{
    [SettingsKey(typeof(CodeEditingSettings), "Unity plugin settings")]
    public class UnitySettings
    {
        [SettingsEntry(true, "If this option is enabled, the Rider Unity editor plugin will be automatically installed and updated.")]
        public bool InstallUnity3DRiderPlugin;

        [SettingsEntry(true, "If this option is enabled, Rider will automatically notify the Unity editor to refresh assets.")]
        public bool AllowAutomaticRefreshInUnity;

        [SettingsEntry(true, "Enables syntax error highlighting, brace matching and more of ShaderLab files.")]
        public bool EnableShaderLabParsing;

        [SettingsEntry(true, "Enables completion based on words found in the current file.")]
        public bool EnableShaderLabHippieCompletion;

        [SettingsEntry(false, "Enables syntax error highlighting of CG blocks in ShaderLab files.")]
        public bool EnableCgErrorHighlighting;
        
        [SettingsEntry(true, "Enables underscore highlighting of costly methods and indirect calls of these methods.")]
        public bool EnablePerformanceCriticalCodeHighlighting;
    }
}
