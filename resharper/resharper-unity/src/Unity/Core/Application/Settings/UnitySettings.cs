using JetBrains.Application.Settings;
using JetBrains.ReSharper.Plugins.Unity.Resources;
using JetBrains.ReSharper.Resources.Settings;

namespace JetBrains.ReSharper.Plugins.Unity.Core.Application.Settings
{
    // TODO: Should all of these settings be under CodeEditingSettings?
    // See also CodeInspectionSettings
    // CodeStyleSettings (for AdditionalFileLayoutSettings)
    // What about debugger settings?
    // Where should the plugin/refresh/merge settings live?
    [SettingsKey(typeof(CodeEditingSettings), DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnitySettings_t_Unity_plugin_settings))]
    public class UnitySettings
    {
        [SettingsEntry(true, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnitySettings_t_If_this_option_is_enabled__the_Rider_Unity_editor_plugin_will_be_automatically_installed_and_updated_))]
        public bool InstallUnity3DRiderPlugin;
        
        [SettingsEntry(true, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnitySettings_t_If_this_option_is_disabled__Rider_package_update_notifications_would_never_be_shown_))]
        public bool AllowRiderUpdateNotifications;

        [SettingsEntry(true, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnitySettings_t_If_this_option_is_enabled__Rider_will_automatically_notify_the_Unity_editor_to_refresh_assets_))]
        public bool AllowAutomaticRefreshInUnity;

        [SettingsEntry(true, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnitySettings_t_If_this_option_is_enabled__UnityYamlMerge_would_be_used_to_merge_YAML_files_))]
        public bool UseUnityYamlMerge;

        // -p         Use premerging
        // -h         Use 'headless' mode (no error dialogs)
        // --fallback Spec file defining fallback tools on conflicts if not using builtin. Can be set to 'none' to disable fallback.
        [SettingsEntry("merge -p -h --fallback none %3 %2 %1 %4", DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnitySettings_t_Merge_parameters))]
        public readonly string MergeParameters;
        
        [SettingsEntry(true, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnitySettings_t_Enables_syntax_error_highlighting__brace_matching_and_more_of_ShaderLab_files_))]
        public bool EnableShaderLabParsing;

        [SettingsEntry(true, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnitySettings_t_Enables_completion_based_on_words_found_in_the_current_file_))]
        public bool EnableShaderLabHippieCompletion;

        [SettingsEntry(false, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnitySettings_t_Enables_syntax_error_highlighting_of_CG_blocks_in_ShaderLab_files_))]
        public bool EnableCgErrorHighlighting;

        [SettingsEntry(false, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnitySettings_t_Suppress_resolve_errors_in_HLSL_))]
        public bool SuppressShaderErrorHighlighting;

        [SettingsEntry(false, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnitySettings_t_Suppress_resolve_errors_in_render_pipeline_package_in_HLSL_))]
        public bool SuppressShaderErrorHighlightingInRenderPipelinePackages;

        [SettingsEntry(GutterIconMode.CodeInsightDisabled, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnitySettings_t_Unity_highlighter_scheme_for_editor_))]
        public GutterIconMode GutterIconMode;

        // backward compability
        [SettingsEntry(true, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnitySettings_t_Should_yaml_heuristic_be_applied_))]
        public bool EnableAssetIndexingPerformanceHeuristic;

        [SettingsEntry(true, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnitySettings_t_Enables_asset_indexing))]
        public bool IsAssetIndexingEnabled;

        [SettingsEntry(true, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnitySettings_t_Prefab_cache))]
        public bool IsPrefabCacheEnabled;

        // Analysis
        [SettingsEntry(true, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnitySettings_t_Enables_performance_analysis_in_frequently_called_code))]
        public bool EnablePerformanceCriticalCodeHighlighting;

        [SettingsEntry(true, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnitySettings_t_Enable_analysis_for_Burst_compiler_issues))]
        public bool EnableBurstCodeHighlighting;

        [SettingsEntry(true, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnitySettings_t_Enables_showing_Unity_icon_for_Burst_compiled_code))]
        public bool EnableIconsForBurstCode;

        // UX for performance critical analysis
        [SettingsEntry(PerformanceHighlightingMode.CurrentMethod, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnitySettings_t_Highlighting_mode_for_performance_critical_code))]
        public PerformanceHighlightingMode PerformanceHighlightingMode;

        [SettingsEntry(true, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnitySettings_t_Enables_showing_hot_icon_for_frequently_called_code))]
        public bool EnableIconsForPerformanceCriticalCode;

        [SettingsEntry(true, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnitySettings_t_Show_Inspector_properties_changes_in_editor))]
        public bool EnableInspectorPropertiesEditor;

        // Debugging
        [SettingsEntry(true, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnitySettings_t_Enable_debugger_extensions))]
        public bool EnableDebuggerExtensions;

        [SettingsEntry(true, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnitySettings_t_Ignore__Break_on_Unhandled_Exceptions__for_IL2CPP_players))]
        public bool IgnoreBreakOnUnhandledExceptionsForIl2Cpp;
                
        [SettingsEntry(true, DescriptionResourceType: typeof(Strings), DescriptionResourceName: nameof(Strings.UnityOptionPage_AddNamingSubSection_Enable_SerializedField_Naming_Rule))]
        public bool EnableSerializedFieldNamingRule;
    }
}
