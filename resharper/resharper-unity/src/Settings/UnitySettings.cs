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

        [SettingsEntry(GutterIconMode.CodeInsightDisabled, "Unity highlighter scheme for editor.")]
        public GutterIconMode GutterIconMode;

        // backward compability
        [SettingsEntry(true, "Should yaml heuristic be applied?")]
        public bool ShouldApplyYamlHugeFileHeuristic;

        [SettingsEntry(true, "Enables syntax error highlighting, brace matching and more of YAML files for Unity")]
        public bool IsYamlParsingEnabled;

        // Analysis
        [SettingsEntry(true, "Enables performance analysis in frequently called code")]
        public bool EnablePerformanceCriticalCodeHighlighting;

        // UX for performance critical analysis
        [SettingsEntry(PerformanceHighlightingMode.CurrentMethod, "Highlighting mode for performance critical code")]
        public PerformanceHighlightingMode PerformanceHighlightingMode;

        [SettingsEntry(true, "Enables showing hot icon for frequently called code")]
        public bool EnableIconsForPerformanceCriticalCode;
        
        [SettingsEntry(true, "Show Inspector properties changes in editor")]
        public bool EnableInspectorPropertiesEditor;
    }
}
