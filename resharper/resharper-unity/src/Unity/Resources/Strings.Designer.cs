namespace JetBrains.ReSharper.Plugins.Unity.Resources
{
  using System;
  using JetBrains.Application.I18n;
  using JetBrains.DataFlow;
  using JetBrains.Diagnostics;
  using JetBrains.Lifetimes;
  using JetBrains.Util;
  using JetBrains.Util.Logging;
  
  [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
  [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
  public static class Strings
  {
    private static readonly ILogger ourLog = Logger.GetLogger("JetBrains.ReSharper.Plugins.Unity.Resources.Strings");

    static Strings()
    {
      CultureContextComponent.Instance.WhenNotNull(Lifetime.Eternal, (lifetime, instance) =>
      {
        lifetime.Bracket(() =>
          {
            ourResourceManager = new Lazy<JetResourceManager>(
              () =>
              {
                return instance
                  .CreateResourceManager("JetBrains.ReSharper.Plugins.Unity.Resources.Strings", typeof(Strings).Assembly);
              });
          },
          () =>
          {
            ourResourceManager = null;
          });
      });
    }
    
    private static Lazy<JetResourceManager> ourResourceManager = null;
    
    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
    public static JetResourceManager ResourceManager
    {
      get
      {
        var resourceManager = ourResourceManager;
        if (resourceManager == null)
        {
          return ErrorJetResourceManager.Instance;
        }
        return resourceManager.Value;
      }
    }

    public static string RiderPackageUpdateAvailabilityChecker_ShowNotificationIfNeeded_JetBrains_Rider_package_in_Unity_is_missing_ => ResourceManager.GetString("RiderPackageUpdateAvailabilityChecker_ShowNotificationIfNeeded_JetBrains_Rider_package_in_Unity_is_missing_");
    public static string RiderPackageUpdateAvailabilityChecker_ShowNotificationIfNeeded_Make_sure_JetBrains_Rider_package_is_installed_in_Unity_Package_Manager_ => ResourceManager.GetString("RiderPackageUpdateAvailabilityChecker_ShowNotificationIfNeeded_Make_sure_JetBrains_Rider_package_is_installed_in_Unity_Package_Manager_");
    public static string DisabledIndexingOfUnityAssets_Text => ResourceManager.GetString("DisabledIndexingOfUnityAssets_Text");
    public static string DueToTheSizeOfTheProjectIndexingOfUnity_Text => ResourceManager.GetString("DueToTheSizeOfTheProjectIndexingOfUnity_Text");
    public static string TurnOnAnyway_Text => ResourceManager.GetString("TurnOnAnyway_Text");
    public static string BurstCompiledCode_Text => ResourceManager.GetString("BurstCompiledCode_Text");
    public static string ConvertToNamedAssemblyDefinitionReference_Name => ResourceManager.GetString("ConvertToNamedAssemblyDefinitionReference_Name");
    public static string ConvertToNamedAssemblyDefinitionReference_Description => ResourceManager.GetString("ConvertToNamedAssemblyDefinitionReference_Description");
    public static string ConvertToNamedReferenceContextAction_Text_To_named_reference => ResourceManager.GetString("ConvertToNamedReferenceContextAction_Text_To_named_reference");
    public static string AsmDefGuidReferenceInlayHintHighlighting_ContextMenuTitle_GUID_Reference_Hints => ResourceManager.GetString("AsmDefGuidReferenceInlayHintHighlighting_ContextMenuTitle_GUID_Reference_Hints");
    public static string AsmDefIntraTextAdornmentModel_BuildContextMenuItems_Configure___ => ResourceManager.GetString("AsmDefIntraTextAdornmentModel_BuildContextMenuItems_Configure___");
    public static string AsmDefPackageVersionInlayHintHighlighting_ContextMenuTitle_Package_Version_Hints => ResourceManager.GetString("AsmDefPackageVersionInlayHintHighlighting_ContextMenuTitle_Package_Version_Hints");
    public static string RemoveInvalidArrayItemQuickFix_Text_Remove_invalid_value => ResourceManager.GetString("RemoveInvalidArrayItemQuickFix_Text_Remove_invalid_value");
    public static string RenameFileToMatchAssemblyNameQuickFix_ExecutePsiTransaction_File___0___already_exists => ResourceManager.GetString("RenameFileToMatchAssemblyNameQuickFix_ExecutePsiTransaction_File___0___already_exists");
    public static string RenameFileToMatchAssemblyNameQuickFix_ExecutePsiTransaction_Cannot_rename___0__ => ResourceManager.GetString("RenameFileToMatchAssemblyNameQuickFix_ExecutePsiTransaction_Cannot_rename___0__");
    public static string RenameFileToMatchAssemblyNameQuickFix_Text_Rename_file_to_match_assembly_name => ResourceManager.GetString("RenameFileToMatchAssemblyNameQuickFix_Text_Rename_file_to_match_assembly_name");
    public static string AsmDefProjectFileType_AsmDefProjectFileType_Assembly_Definition__Unity_ => ResourceManager.GetString("AsmDefProjectFileType_AsmDefProjectFileType_Assembly_Definition__Unity_");
    public static string AsmRefProjectFileType_AsmRefProjectFileType_Assembly_Definition_Reference__Unity_ => ResourceManager.GetString("AsmRefProjectFileType_AsmRefProjectFileType_Assembly_Definition_Reference__Unity_");
    public static string UnityInlayHintSettings_t_Inlay_hint_settings_for_Unity_related_files => ResourceManager.GetString("UnityInlayHintSettings_t_Inlay_hint_settings_for_Unity_related_files");
    public static string UnityInlayHintSettings_t_Visibility_mode_of_hints_for_GUID_references_in__asmdef_files => ResourceManager.GetString("UnityInlayHintSettings_t_Visibility_mode_of_hints_for_GUID_references_in__asmdef_files");
    public static string UnityInlayHintSettings_t_Visibility_mode_of_hints_for_package_versions_in__asmdef_files => ResourceManager.GetString("UnityInlayHintSettings_t_Visibility_mode_of_hints_for_package_versions_in__asmdef_files");
    public static string UnitySettings_t_Unity_plugin_settings => ResourceManager.GetString("UnitySettings_t_Unity_plugin_settings");
    public static string UnitySettings_t_If_this_option_is_enabled__the_Rider_Unity_editor_plugin_will_be_automatically_installed_and_updated_ => ResourceManager.GetString("UnitySettings_t_If_this_option_is_enabled__the_Rider_Unity_editor_plugin_will_be_automatically_installed_and_updated_");
    public static string UnitySettings_t_If_this_option_is_disabled__Rider_package_update_notifications_would_never_be_shown_ => ResourceManager.GetString("UnitySettings_t_If_this_option_is_disabled__Rider_package_update_notifications_would_never_be_shown_");
    public static string UnitySettings_t_If_this_option_is_enabled__Rider_will_automatically_notify_the_Unity_editor_to_refresh_assets_ => ResourceManager.GetString("UnitySettings_t_If_this_option_is_enabled__Rider_will_automatically_notify_the_Unity_editor_to_refresh_assets_");
    public static string UnitySettings_t_If_this_option_is_enabled__UnityYamlMerge_would_be_used_to_merge_YAML_files_ => ResourceManager.GetString("UnitySettings_t_If_this_option_is_enabled__UnityYamlMerge_would_be_used_to_merge_YAML_files_");
    public static string UnitySettings_t_Merge_parameters => ResourceManager.GetString("UnitySettings_t_Merge_parameters");
    public static string UnitySettings_t_Enables_syntax_error_highlighting__brace_matching_and_more_of_ShaderLab_files_ => ResourceManager.GetString("UnitySettings_t_Enables_syntax_error_highlighting__brace_matching_and_more_of_ShaderLab_files_");
    public static string UnitySettings_t_Enables_completion_based_on_words_found_in_the_current_file_ => ResourceManager.GetString("UnitySettings_t_Enables_completion_based_on_words_found_in_the_current_file_");
    public static string UnitySettings_t_Enables_syntax_error_highlighting_of_CG_blocks_in_ShaderLab_files_ => ResourceManager.GetString("UnitySettings_t_Enables_syntax_error_highlighting_of_CG_blocks_in_ShaderLab_files_");
    public static string UnitySettings_t_Suppress_resolve_errors_in_HLSL_ => ResourceManager.GetString("UnitySettings_t_Suppress_resolve_errors_in_HLSL_");
    public static string UnitySettings_t_Suppress_resolve_errors_in_render_pipeline_package_in_HLSL_ => ResourceManager.GetString("UnitySettings_t_Suppress_resolve_errors_in_render_pipeline_package_in_HLSL_");
    public static string UnitySettings_t_Unity_highlighter_scheme_for_editor_ => ResourceManager.GetString("UnitySettings_t_Unity_highlighter_scheme_for_editor_");
    public static string UnitySettings_t_Should_yaml_heuristic_be_applied_ => ResourceManager.GetString("UnitySettings_t_Should_yaml_heuristic_be_applied_");
    public static string UnitySettings_t_Enables_asset_indexing => ResourceManager.GetString("UnitySettings_t_Enables_asset_indexing");
    public static string UnitySettings_t_Prefab_cache => ResourceManager.GetString("UnitySettings_t_Prefab_cache");
    public static string UnitySettings_t_Enables_performance_analysis_in_frequently_called_code => ResourceManager.GetString("UnitySettings_t_Enables_performance_analysis_in_frequently_called_code");
    public static string UnitySettings_t_Enable_analysis_for_Burst_compiler_issues => ResourceManager.GetString("UnitySettings_t_Enable_analysis_for_Burst_compiler_issues");
    public static string UnitySettings_t_Enables_showing_Unity_icon_for_Burst_compiled_code => ResourceManager.GetString("UnitySettings_t_Enables_showing_Unity_icon_for_Burst_compiled_code");
    public static string UnitySettings_t_Highlighting_mode_for_performance_critical_code => ResourceManager.GetString("UnitySettings_t_Highlighting_mode_for_performance_critical_code");
    public static string UnitySettings_t_Enables_showing_hot_icon_for_frequently_called_code => ResourceManager.GetString("UnitySettings_t_Enables_showing_hot_icon_for_frequently_called_code");
    public static string UnitySettings_t_Show_Inspector_properties_changes_in_editor => ResourceManager.GetString("UnitySettings_t_Show_Inspector_properties_changes_in_editor");
    public static string UnitySettings_t_Enable_debugger_extensions => ResourceManager.GetString("UnitySettings_t_Enable_debugger_extensions");
    public static string UnitySettings_t_Ignore__Break_on_Unhandled_Exceptions__for_IL2CPP_players => ResourceManager.GetString("UnitySettings_t_Ignore__Break_on_Unhandled_Exceptions__for_IL2CPP_players");
  }
}