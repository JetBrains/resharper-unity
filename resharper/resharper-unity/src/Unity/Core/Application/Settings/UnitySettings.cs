using JetBrains.Application.Settings;
using JetBrains.ReSharper.Resources.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings
{
    // TODO: Should all of these settings be under CodeEditingSettings?
    // See also CodeInspectionSettings
    // CodeStyleSettings (for AdditionalFileLayoutSettings)
    // What about debugger settings?
    // Where should the plugin/refresh/merge settings live?
    [SettingsKey(typeof(CodeEditingSettings), "Unity plugin settings")]
    public class UnitySettings
    {
        [SettingsEntry(true, "If this option is enabled, the Rider Unity editor plugin will be automatically installed and updated.")]
        public bool InstallUnity3DRiderPlugin;
        
        [SettingsEntry(true, "If this option is disabled, Rider package update notifications would never be shown.")]
        public bool AllowRiderUpdateNotifications;

        [SettingsEntry(true, "If this option is enabled, Rider will automatically notify the Unity editor to refresh assets.")]
        public bool AllowAutomaticRefreshInUnity;

        [SettingsEntry(true, "If this option is enabled, UnityYamlMerge would be used to merge YAML files.")]
        public bool UseUnityYamlMerge;

        // -p         Use premerging
        // -h         Use 'headless' mode (no error dialogs)
        // --fallback Spec file defining fallback tools on conflicts if not using builtin. Can be set to 'none' to disable fallback.
        [SettingsEntry("merge -p -h --fallback none %3 %2 %1 %4", "Merge parameters")]
        public readonly string MergeParameters;

        [SettingsEntry(true, "Enables syntax error highlighting, brace matching and more of ShaderLab files.")]
        public bool EnableShaderLabParsing;

        [SettingsEntry(true, "Enables completion based on words found in the current file.")]
        public bool EnableShaderLabHippieCompletion;

        [SettingsEntry(false, "Enables syntax error highlighting of CG blocks in ShaderLab files.")]
        public bool EnableCgErrorHighlighting;

        [SettingsEntry(false, "Suppress resolve errors in HLSL.")]
        public bool SuppressShaderErrorHighlighting;

        [SettingsEntry(false, "Suppress resolve errors in render-pipeline package in HLSL.")]
        public bool SuppressShaderErrorHighlightingInRenderPipelinePackages;

        [SettingsEntry(GutterIconMode.CodeInsightDisabled, "Unity highlighter scheme for editor.")]
        public GutterIconMode GutterIconMode;

        // backward compability
        [SettingsEntry(true, "Should yaml heuristic be applied?")]
        public bool EnableAssetIndexingPerformanceHeuristic;

        [SettingsEntry(true, "Enables asset indexing")]
        public bool IsAssetIndexingEnabled;

        [SettingsEntry(true, "Prefab cache")]
        public bool IsPrefabCacheEnabled;

        // Analysis
        [SettingsEntry(true, "Enables performance analysis in frequently called code")]
        public bool EnablePerformanceCriticalCodeHighlighting;

        [SettingsEntry(true, "Enable analysis for Burst compiler issues")]
        public bool EnableBurstCodeHighlighting;

        [SettingsEntry(true, "Enables showing Unity icon for Burst compiled code")]
        public bool EnableIconsForBurstCode;

        // UX for performance critical analysis
        [SettingsEntry(PerformanceHighlightingMode.CurrentMethod, "Highlighting mode for performance critical code")]
        public PerformanceHighlightingMode PerformanceHighlightingMode;

        [SettingsEntry(true, "Enables showing hot icon for frequently called code")]
        public bool EnableIconsForPerformanceCriticalCode;

        [SettingsEntry(true, "Show Inspector properties changes in editor")]
        public bool EnableInspectorPropertiesEditor;

        // Debugging
        [SettingsEntry(true, "Enable debugger extensions")]
        public bool EnableDebuggerExtensions;

        [SettingsEntry(true, "Ignore 'Break on Unhandled Exceptions' for IL2CPP players")]
        public bool IgnoreBreakOnUnhandledExceptionsForIl2Cpp;
    }
}
