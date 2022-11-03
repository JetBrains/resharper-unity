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
    public static string RenameFileToMatchAssemblyNameQuickFix_ExecutePsiTransaction_File___NewFileName___already_exists => ResourceManager.GetString("RenameFileToMatchAssemblyNameQuickFix_ExecutePsiTransaction_File___NewFileName___already_exists");
    public static string RenameFileToMatchAssemblyNameQuickFix_ExecutePsiTransaction_Cannot_rename___FileName__ => ResourceManager.GetString("RenameFileToMatchAssemblyNameQuickFix_ExecutePsiTransaction_Cannot_rename___FileName__");
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
    public static string UnityInlayHintsOptionsPage_UnityInlayHintsOptionsPage_Assembly_Definition_file_GUID_references => ResourceManager.GetString("UnityInlayHintsOptionsPage_UnityInlayHintsOptionsPage_Assembly_Definition_file_GUID_references");
    public static string UnityInlayHintsOptionsPage_UnityInlayHintsOptionsPage_Assembly_Definition_file_package_versions => ResourceManager.GetString("UnityInlayHintsOptionsPage_UnityInlayHintsOptionsPage_Assembly_Definition_file_package_versions");
    public static string UnityOptionsPage_AddGeneralSection_General => ResourceManager.GetString("UnityOptionsPage_AddGeneralSection_General");
    public static string UnityOptionsPage_AddGeneralSection_Automatically_install_and_update_Rider_s_Unity_editor_plugin => ResourceManager.GetString("UnityOptionsPage_AddGeneralSection_Automatically_install_and_update_Rider_s_Unity_editor_plugin");
    public static string UnityOptionsPage_AddGeneralSection_ => ResourceManager.GetString("UnityOptionsPage_AddGeneralSection_");
    public static string UnityOptionsPage_AddGeneralSection_Automatically_refresh_assets_in_Unity => ResourceManager.GetString("UnityOptionsPage_AddGeneralSection_Automatically_refresh_assets_in_Unity");
    public static string UnityOptionsPage_AddGeneralSection_Notify_when_Rider_package_update_is_available => ResourceManager.GetString("UnityOptionsPage_AddGeneralSection_Notify_when_Rider_package_update_is_available");
    public static string UnityOptionsPage_AddCSharpSection_Show_gutter_icons_for_implicit_script_usages_ => ResourceManager.GetString("UnityOptionsPage_AddCSharpSection_Show_gutter_icons_for_implicit_script_usages_");
    public static string UnityOptionsPage_AddCSharpSection_Always => ResourceManager.GetString("UnityOptionsPage_AddCSharpSection_Always");
    public static string UnityOptionsPage_AddPerformanceAnalysisSubSection_Current_method_only => ResourceManager.GetString("UnityOptionsPage_AddPerformanceAnalysisSubSection_Current_method_only");
    public static string UnityOptionsPage_AddPerformanceAnalysisSubSection_Never => ResourceManager.GetString("UnityOptionsPage_AddPerformanceAnalysisSubSection_Never");
    public static string UnityOptionsPage_AddCSharpSection_When_Code_Vision_is_disabled => ResourceManager.GetString("UnityOptionsPage_AddCSharpSection_When_Code_Vision_is_disabled");
    public static string UnityOptionsPage_AddCSharpSection_Show_gutter_icons_for_implicit_script_usages => ResourceManager.GetString("UnityOptionsPage_AddCSharpSection_Show_gutter_icons_for_implicit_script_usages");
    public static string UnityOptionsPage_AddPerformanceAnalysisSubSection_Enable_performance_analysis_in_frequently_called_code => ResourceManager.GetString("UnityOptionsPage_AddPerformanceAnalysisSubSection_Enable_performance_analysis_in_frequently_called_code");
    public static string UnityOptionsPage_AddPerformanceAnalysisSubSection_Highlight_performance_critical_contexts_ => ResourceManager.GetString("UnityOptionsPage_AddPerformanceAnalysisSubSection_Highlight_performance_critical_contexts_");
    public static string UnityOptionsPage_AddPerformanceAnalysisSubSection_Show_gutter_icons_for_frequently_called_methods => ResourceManager.GetString("UnityOptionsPage_AddPerformanceAnalysisSubSection_Show_gutter_icons_for_frequently_called_methods");
    public static string UnityOptionsPage_AddBurstAnalysisSubSection_Enable_analysis_for_Burst_compiler_issues => ResourceManager.GetString("UnityOptionsPage_AddBurstAnalysisSubSection_Enable_analysis_for_Burst_compiler_issues");
    public static string UnityOptionsPage_AddBurstAnalysisSubSection_Show_gutter_icons_for_Burst_compiled_called_methods => ResourceManager.GetString("UnityOptionsPage_AddBurstAnalysisSubSection_Show_gutter_icons_for_Burst_compiled_called_methods");
    public static string UnityOptionsPage_AddNamingSubSection_Serialized_field_naming_rules => ResourceManager.GetString("UnityOptionsPage_AddNamingSubSection_Serialized_field_naming_rules");
    public static string UnityOptionsPage_AddNamingSubSection_Prefix_ => ResourceManager.GetString("UnityOptionsPage_AddNamingSubSection_Prefix_");
    public static string UnityOptionsPage_AddNamingSubSection_Suffix_ => ResourceManager.GetString("UnityOptionsPage_AddNamingSubSection_Suffix_");
    public static string UnityOptionsPage_AddNamingSubSection_Style_ => ResourceManager.GetString("UnityOptionsPage_AddNamingSubSection_Style_");
    public static string UnityOptionsPage_AddNamingSubSection_Enable_inspection => ResourceManager.GetString("UnityOptionsPage_AddNamingSubSection_Enable_inspection");
    public static string UnityOptionsPage_AddNamingSubSection_UpperCamelCase => ResourceManager.GetString("UnityOptionsPage_AddNamingSubSection_UpperCamelCase");
    public static string UnityOptionsPage_AddNamingSubSection_UpperCamelCase_UnderscoreTolerant => ResourceManager.GetString("UnityOptionsPage_AddNamingSubSection_UpperCamelCase_UnderscoreTolerant");
    public static string UnityOptionsPage_AddNamingSubSection_UpperCamelCase_underscoreTolerant2 => ResourceManager.GetString("UnityOptionsPage_AddNamingSubSection_UpperCamelCase_underscoreTolerant2");
    public static string UnityOptionsPage_AddNamingSubSection_lowerCamelCase => ResourceManager.GetString("UnityOptionsPage_AddNamingSubSection_lowerCamelCase");
    public static string UnityOptionsPage_AddNamingSubSection_lowerCamelCase_UnderscoreTolerant => ResourceManager.GetString("UnityOptionsPage_AddNamingSubSection_lowerCamelCase_UnderscoreTolerant");
    public static string UnityOptionsPage_AddNamingSubSection_lowerCamelCase_underscoreTolerant2 => ResourceManager.GetString("UnityOptionsPage_AddNamingSubSection_lowerCamelCase_underscoreTolerant2");
    public static string UnityOptionsPage_AddNamingSubSection_ALL_UPPER => ResourceManager.GetString("UnityOptionsPage_AddNamingSubSection_ALL_UPPER");
    public static string UnityOptionsPage_AddNamingSubSection_First_upper => ResourceManager.GetString("UnityOptionsPage_AddNamingSubSection_First_upper");
    public static string UnityOptionsPage_AddTextBasedAssetsSection_Text_based_assets => ResourceManager.GetString("UnityOptionsPage_AddTextBasedAssetsSection_Text_based_assets");
    public static string UnityOptionsPage_AddTextBasedAssetsSection_Parse_text_based_asset_files_for_script_and_event_handler_usages => ResourceManager.GetString("UnityOptionsPage_AddTextBasedAssetsSection_Parse_text_based_asset_files_for_script_and_event_handler_usages");
    public static string UnityOptionsPage_AddTextBasedAssetsSection_Show_Inspector_values_in_the_editor => ResourceManager.GetString("UnityOptionsPage_AddTextBasedAssetsSection_Show_Inspector_values_in_the_editor");
    public static string UnityOptionsPage_AddTextBasedAssetsSection_Cache_prefab_data_to_improve_find_usage_performance => ResourceManager.GetString("UnityOptionsPage_AddTextBasedAssetsSection_Cache_prefab_data_to_improve_find_usage_performance");
    public static string UnityOptionsPage_AddTextBasedAssetsSection_Automatically_disable_asset_indexing_for_large_solutions => ResourceManager.GetString("UnityOptionsPage_AddTextBasedAssetsSection_Automatically_disable_asset_indexing_for_large_solutions");
    public static string UnityOptionsPage_AddTextBasedAssetsSection_Prefer_UnityYamlMerge_for_merging_YAML_files => ResourceManager.GetString("UnityOptionsPage_AddTextBasedAssetsSection_Prefer_UnityYamlMerge_for_merging_YAML_files");
    public static string UnityOptionsPage_AddTextBasedAssetsSection_Merge_parameters => ResourceManager.GetString("UnityOptionsPage_AddTextBasedAssetsSection_Merge_parameters");
    public static string UnityOptionsPage_AddShadersSection_Shaders => ResourceManager.GetString("UnityOptionsPage_AddShadersSection_Shaders");
    public static string UnityOptionsPage_AddShadersSection_Suppress_resolve_errors_of_unqualified_names => ResourceManager.GetString("UnityOptionsPage_AddShadersSection_Suppress_resolve_errors_of_unqualified_names");
    public static string UnityOptionsPage_AddDebuggingSection_Debugging => ResourceManager.GetString("UnityOptionsPage_AddDebuggingSection_Debugging");
    public static string UnityOptionsPage_AddDebuggingSection_Extend_value_rendering => ResourceManager.GetString("UnityOptionsPage_AddDebuggingSection_Extend_value_rendering");
    public static string UnityOptionsPage_AddDebuggingSection_Extend_value_rendering_Comment => ResourceManager.GetString("UnityOptionsPage_AddDebuggingSection_Extend_value_rendering_Comment");
    public static string UnityOptionsPage_AddDebuggingSection_Ignore__Break_on_unhandled_exceptions__setting_for_IL2CPP_players => ResourceManager.GetString("UnityOptionsPage_AddDebuggingSection_Ignore__Break_on_unhandled_exceptions__setting_for_IL2CPP_players");
    public static string UnityOptionsPage_AddDebuggingSection_Break_on_unhandled_exceptions__setting_for_IL2CPP_players_Comment => ResourceManager.GetString("UnityOptionsPage_AddDebuggingSection_Break_on_unhandled_exceptions__setting_for_IL2CPP_players_Comment");
    public static string UnityOptionsPage_AddInternalSection_Internal => ResourceManager.GetString("UnityOptionsPage_AddInternalSection_Internal");
    public static string UnityOptionsPage_AddInternalSection_Suppress_resolve_errors_in_render_pipeline_packages => ResourceManager.GetString("UnityOptionsPage_AddInternalSection_Suppress_resolve_errors_in_render_pipeline_packages");
    public static string UnityOptionsPage_AddInternalSection__Deprecated__Parse_GLSL_files_for_syntax_errors__requires_internal_mode__and_re_opening_solution_ => ResourceManager.GetString("UnityOptionsPage_AddInternalSection__Deprecated__Parse_GLSL_files_for_syntax_errors__requires_internal_mode__and_re_opening_solution_");
    public static string UnityFindUsagesProvider_GetNotFoundMessage_SearchRequestLocalizedTitle_are_only_implicit_ => ResourceManager.GetString("UnityFindUsagesProvider_GetNotFoundMessage_SearchRequestLocalizedTitle_are_only_implicit_");
    public static string DuplicateMenuItemShortCutProblemAnalyzer_Analyze_this_file => ResourceManager.GetString("DuplicateMenuItemShortCutProblemAnalyzer_Analyze_this_file");
    public static string AsmDefOccurrenceKindProvider_AssemblyDefinitionReference_Assembly_definition_reference => ResourceManager.GetString("AsmDefOccurrenceKindProvider_AssemblyDefinitionReference_Assembly_definition_reference");
    public static string UnityAssetSpecificOccurrenceKinds_EventHandler_Unity_event_handler => ResourceManager.GetString("UnityAssetSpecificOccurrenceKinds_EventHandler_Unity_event_handler");
    public static string UnityAssetSpecificOccurrenceKinds_ComponentUsage_Unity_component_usage => ResourceManager.GetString("UnityAssetSpecificOccurrenceKinds_ComponentUsage_Unity_component_usage");
    public static string UnityAssetSpecificOccurrenceKinds_InspectorUsage_Inspector_values => ResourceManager.GetString("UnityAssetSpecificOccurrenceKinds_InspectorUsage_Inspector_values");
    public static string MetaProjectFileType_MetaProjectFileType_Unity_Meta_File => ResourceManager.GetString("MetaProjectFileType_MetaProjectFileType_Unity_Meta_File");
    public static string UnityYamlProjectFileType_UnityYamlProjectFileType_Unity_Yaml => ResourceManager.GetString("UnityYamlProjectFileType_UnityYamlProjectFileType_Unity_Yaml");
    public static string ConvertToGuidReferenceQuickFix_Text_To_GUID_reference => ResourceManager.GetString("ConvertToGuidReferenceQuickFix_Text_To_GUID_reference");
    public static string PreferGenericMethodOverloadQuickFix_Text_Convert_to__MethodName__1__ => ResourceManager.GetString("PreferGenericMethodOverloadQuickFix_Text_Convert_to__MethodName__1__");
    public static string PreferGenericMethodOverloadQuickFix_ScopedText_Use_strongly_typed_overloads => ResourceManager.GetString("PreferGenericMethodOverloadQuickFix_ScopedText_Use_strongly_typed_overloads");
    public static string DocumentationNavigationAction_Text_View_documentation => ResourceManager.GetString("DocumentationNavigationAction_Text_View_documentation");
    public static string ConvertFromCoroutineBulbAction_Text_To_standard_event_function => ResourceManager.GetString("ConvertFromCoroutineBulbAction_Text_To_standard_event_function");
    public static string ConvertToCoroutineBulbAction_Text_To_coroutine => ResourceManager.GetString("ConvertToCoroutineBulbAction_Text_To_coroutine");
    public static string AddAttributeAction_Text_Add___0__ => ResourceManager.GetString("AddAttributeAction_Text_Add___0__");
    public static string AddAttributeAction_Text_Add___0___before_SelectedField => ResourceManager.GetString("AddAttributeAction_Text_Add___0___before_SelectedField");
    public static string AddAttributeAction_Text_Add___0___before_all_fields => ResourceManager.GetString("AddAttributeAction_Text_Add___0___before_all_fields");
    public static string AddAttributeAction_Text_Add___0___to_SelectedField => ResourceManager.GetString("AddAttributeAction_Text_Add___0___to_SelectedField");
    public static string AddAttributeAction_Text_Add___0___to_all_fields => ResourceManager.GetString("AddAttributeAction_Text_Add___0___to_all_fields");
    public static string RemoveAttributeAction_Text_Remove___0__ => ResourceManager.GetString("RemoveAttributeAction_Text_Remove___0__");
    public static string RemoveAttributeAction_Text_Remove___0___from_SelectedField => ResourceManager.GetString("RemoveAttributeAction_Text_Remove___0___from_SelectedField");
    public static string RemoveAttributeAction_Text_Remove___0___from_all_fields => ResourceManager.GetString("RemoveAttributeAction_Text_Remove___0___from_all_fields");
    public static string AutoPropertyToSerializedBackingFieldAction_Text_To_property_with_serialized_backing_field => ResourceManager.GetString("AutoPropertyToSerializedBackingFieldAction_Text_To_property_with_serialized_backing_field");
    public static string AddTooltipAttributeAction_Text_Convert_to__Tooltip__attribute => ResourceManager.GetString("AddTooltipAttributeAction_Text_Convert_to__Tooltip__attribute");
    public static string AddTooltipAttributeAction_Text_Add__Tooltip__attribute => ResourceManager.GetString("AddTooltipAttributeAction_Text_Add__Tooltip__attribute");
    public static string AddTooltipAttributeAction_Text_Convert_XML_doc_to__Tooltip__attribute => ResourceManager.GetString("AddTooltipAttributeAction_Text_Convert_XML_doc_to__Tooltip__attribute");
    public static string AddTooltipAttributeAction_Text_Add__Tooltip__attribute_from_XML_doc => ResourceManager.GetString("AddTooltipAttributeAction_Text_Add__Tooltip__attribute_from_XML_doc");
    public static string CreateAssetMenuAction_Text_Add_to_Unity_s__Assets_Create__menu => ResourceManager.GetString("CreateAssetMenuAction_Text_Add_to_Unity_s__Assets_Create__menu");
    public static string InitializeComponentBulbActionBase_Text_Initialize_in___0__ => ResourceManager.GetString("InitializeComponentBulbActionBase_Text_Initialize_in___0__");
    public static string AddRequireComponentBulbActionBase_Text_Add__RequireComponent_ => ResourceManager.GetString("AddRequireComponentBulbActionBase_Text_Add__RequireComponent_");
    public static string ToggleSerializedFieldAll_Text_To_serialized_field => ResourceManager.GetString("ToggleSerializedFieldAll_Text_To_serialized_field");
    public static string ToggleSerializedFieldAll_Text_Make__0__non_serialized => ResourceManager.GetString("ToggleSerializedFieldAll_Text_Make__0__non_serialized");
    public static string ToggleSerializedFieldAll_Text_Make__0__serialized => ResourceManager.GetString("ToggleSerializedFieldAll_Text_Make__0__serialized");
    public static string ToggleSerializedFieldAll_Text_all_fields => ResourceManager.GetString("ToggleSerializedFieldAll_Text_all_fields");
    public static string ToggleSerializedFieldAll_Text_field => ResourceManager.GetString("ToggleSerializedFieldAll_Text_field");
    public static string ToggleSerializedFieldAll_Text_Make__0__serialized__remove_static_and_readonly_ => ResourceManager.GetString("ToggleSerializedFieldAll_Text_Make__0__serialized__remove_static_and_readonly_");
    public static string ToggleSerializedFieldAll_Text_Make__0__serialized__remove_static_ => ResourceManager.GetString("ToggleSerializedFieldAll_Text_Make__0__serialized__remove_static_");
    public static string ToggleSerializedFieldAll_Text_Make__0__serialized__remove_readonly_ => ResourceManager.GetString("ToggleSerializedFieldAll_Text_Make__0__serialized__remove_readonly_");
    public static string ToggleSerializedFieldOne_Text_Make_field___0___serialized__remove_static_and_readonly_ => ResourceManager.GetString("ToggleSerializedFieldOne_Text_Make_field___0___serialized__remove_static_and_readonly_");
    public static string ToggleSerializedFieldOne_Text_Make_field___0___serialized__remove_static_ => ResourceManager.GetString("ToggleSerializedFieldOne_Text_Make_field___0___serialized__remove_static_");
    public static string ToggleSerializedFieldOne_Text_Make_field___0___serialized__remove_readonly_ => ResourceManager.GetString("ToggleSerializedFieldOne_Text_Make_field___0___serialized__remove_readonly_");
    public static string ToggleSerializedFieldOne_Text_Make_field___0___non_serialized => ResourceManager.GetString("ToggleSerializedFieldOne_Text_Make_field___0___non_serialized");
    public static string ToggleSerializedFieldOne_Text_Make_field___0___serialized => ResourceManager.GetString("ToggleSerializedFieldOne_Text_Make_field___0___serialized");
    public static string MoveAction_Text_Introduce_field_and_initialise_in___0__ => ResourceManager.GetString("MoveAction_Text_Introduce_field_and_initialise_in___0__");
    public static string MoveFromLoopAction_Text_Move_outside_of_loop => ResourceManager.GetString("MoveFromLoopAction_Text_Move_outside_of_loop");
    public static string CachePropertyValueQuickFix_Text_Introduce_variable => ResourceManager.GetString("CachePropertyValueQuickFix_Text_Introduce_variable");
    public static string ConvertCoalescingToConditionalQuickFix_Text_Convert_to_conditional_expression => ResourceManager.GetString("ConvertCoalescingToConditionalQuickFix_Text_Convert_to_conditional_expression");
    public static string ConvertToCompareTagQuickFix_Text_Convert_to__CompareTag_ => ResourceManager.GetString("ConvertToCompareTagQuickFix_Text_Convert_to__CompareTag_");
    public static string ConvertToGameObjectAddComponentQuickFix_Text_Convert_to__GameObject_AddComponent__0_____ => ResourceManager.GetString("ConvertToGameObjectAddComponentQuickFix_Text_Convert_to__GameObject_AddComponent__0_____");
    public static string ConvertToScriptableObjectCreateInstanceQuickFix_Text_Convert_to__ScriptableObject_CreateInstance__0_____ => ResourceManager.GetString("ConvertToScriptableObjectCreateInstanceQuickFix_Text_Convert_to__ScriptableObject_CreateInstance__0_____");
    public static string CreateSerializedFieldFromUsageAction_Text_Create_Unity_serialized_field___0__ => ResourceManager.GetString("CreateSerializedFieldFromUsageAction_Text_Create_Unity_serialized_field___0__");
    public static string CreateStaticConstructorFromUsageAction_Text_Create_static_constructor___0__ => ResourceManager.GetString("CreateStaticConstructorFromUsageAction_Text_Create_static_constructor___0__");
    public static string FormerlySerializedAsSplitDeclarationsFix_Text_Split_into_separate_declarations => ResourceManager.GetString("FormerlySerializedAsSplitDeclarationsFix_Text_Split_into_separate_declarations");
    public static string GenerateUnityEventFunctionsFix_Text_Generate_Unity_event_functions => ResourceManager.GetString("GenerateUnityEventFunctionsFix_Text_Generate_Unity_event_functions");
    public static string ChangeSignatureBulbAction_GetText_Make___0____1_ => ResourceManager.GetString("ChangeSignatureBulbAction_GetText_Make___0____1_");
    public static string ChangeSignatureBulbAction_GetText_Remove___0___modifier => ResourceManager.GetString("ChangeSignatureBulbAction_GetText_Remove___0___modifier");
    public static string ChangeSignatureBulbAction_GetText_Change_parameters_to____0___ => ResourceManager.GetString("ChangeSignatureBulbAction_GetText_Change_parameters_to____0___");
    public static string ChangeSignatureBulbAction_GetText_Change_return_type_to___0__ => ResourceManager.GetString("ChangeSignatureBulbAction_GetText_Change_return_type_to___0__");
    public static string ChangeSignatureBulbAction_GetText_Remove_type_parameters => ResourceManager.GetString("ChangeSignatureBulbAction_GetText_Remove_type_parameters");
    public static string ChangeSignatureBulbAction_GetText_Change_signature_to___0__ => ResourceManager.GetString("ChangeSignatureBulbAction_GetText_Change_signature_to___0__");
    public static string InefficientMultidimensionalArrayUsageQuickFix_Text_Convert_to_jagged_array => ResourceManager.GetString("InefficientMultidimensionalArrayUsageQuickFix_Text_Convert_to_jagged_array");
    public static string ChangeSceneAtArgumentAction_Text_Change_scene_name_to___0__ => ResourceManager.GetString("ChangeSceneAtArgumentAction_Text_Change_scene_name_to___0__");
    public static string MakeSerializable_Text_Make_type___0___serializable => ResourceManager.GetString("MakeSerializable_Text_Make_type___0___serializable");
    public static string MultiplicationOrderQuickFix_Text_Reorder_multiplication => ResourceManager.GetString("MultiplicationOrderQuickFix_Text_Reorder_multiplication");
    public static string PreferAddressByIdToGraphicsParamsQuickFix_Text_Use_cached_property_index => ResourceManager.GetString("PreferAddressByIdToGraphicsParamsQuickFix_Text_Use_cached_property_index");
    public static string PreferNonAllocApiQuickFix_Text_Convert_to___0__ => ResourceManager.GetString("PreferNonAllocApiQuickFix_Text_Convert_to___0__");
    public static string RemoveAllReadonly_Text_Make_all_fields_non_readonly => ResourceManager.GetString("RemoveAllReadonly_Text_Make_all_fields_non_readonly");
    public static string RemoveAllReadonly_Text_Make_field_non_readonly => ResourceManager.GetString("RemoveAllReadonly_Text_Make_field_non_readonly");
    public static string RemoveOneReadonly_Text_Make_Field_non_readonly => ResourceManager.GetString("RemoveOneReadonly_Text_Make_Field_non_readonly");
    public static string RemoveRedundantAttributeQuickFix_Text_Remove_redundant_attribute => ResourceManager.GetString("RemoveRedundantAttributeQuickFix_Text_Remove_redundant_attribute");
    public static string RemoveRedundantAttributeQuickFix_ScopedText_Remove_redundant_Unity_attributes => ResourceManager.GetString("RemoveRedundantAttributeQuickFix_ScopedText_Remove_redundant_Unity_attributes");
    public static string AutoPropertyToSerializedBackingFieldAction_Name => ResourceManager.GetString("AutoPropertyToSerializedBackingFieldAction_Name");
    public static string AutoPropertyToSerializedBackingFieldAction_Description => ResourceManager.GetString("AutoPropertyToSerializedBackingFieldAction_Description");
    public static string ConvertXmlDocToTooltipAttributeAction_Name => ResourceManager.GetString("ConvertXmlDocToTooltipAttributeAction_Name");
    public static string ConvertXmlDocToTooltipAttributeAction_Description => ResourceManager.GetString("ConvertXmlDocToTooltipAttributeAction_Description");
    public static string AddDiscardAttributeContextAction_Name => ResourceManager.GetString("AddDiscardAttributeContextAction_Name");
    public static string AddExpensiveCommentContextAction_Name => ResourceManager.GetString("AddExpensiveCommentContextAction_Name");
    public static string AddPerformanceAnalysisDisableCommentContextAction_Name => ResourceManager.GetString("AddPerformanceAnalysisDisableCommentContextAction_Name");
    public static string AddHeaderAttributeAction_Name => ResourceManager.GetString("AddHeaderAttributeAction_Name");
    public static string AddHeaderAttributeAction_Description => ResourceManager.GetString("AddHeaderAttributeAction_Description");
    public static string AddRangeAttributeAction_Name => ResourceManager.GetString("AddRangeAttributeAction_Name");
    public static string AddRangeAttributeAction_Description => ResourceManager.GetString("AddRangeAttributeAction_Description");
    public static string AddSpaceAttributeAction_Name => ResourceManager.GetString("AddSpaceAttributeAction_Name");
    public static string AddSpaceAttributeAction_Description => ResourceManager.GetString("AddSpaceAttributeAction_Description");
    public static string AddTooltipAttributeAction_Name => ResourceManager.GetString("AddTooltipAttributeAction_Name");
    public static string AddTooltipAttributeAction_Description => ResourceManager.GetString("AddTooltipAttributeAction_Description");
    public static string CreateAssetMenuContextAction_Name => ResourceManager.GetString("CreateAssetMenuContextAction_Name");
    public static string CreateAssetMenuContextAction_Description => ResourceManager.GetString("CreateAssetMenuContextAction_Description");
    public static string GenerateUnityEventFunctionsAction_Name => ResourceManager.GetString("GenerateUnityEventFunctionsAction_Name");
    public static string GenerateUnityEventFunctionsAction_Description => ResourceManager.GetString("GenerateUnityEventFunctionsAction_Description");
    public static string InitializeFieldComponentContextAction_Name => ResourceManager.GetString("InitializeFieldComponentContextAction_Name");
    public static string InitializeFieldComponentContextAction_Description => ResourceManager.GetString("InitializeFieldComponentContextAction_Description");
    public static string InitializePropertyComponentContextAction_Name => ResourceManager.GetString("InitializePropertyComponentContextAction_Name");
    public static string InitializePropertyComponentContextAction_Description => ResourceManager.GetString("InitializePropertyComponentContextAction_Description");
    public static string ToggleHideInInspectorAttributeAction_Name => ResourceManager.GetString("ToggleHideInInspectorAttributeAction_Name");
    public static string ToggleHideInInspectorAttributeAction_Description => ResourceManager.GetString("ToggleHideInInspectorAttributeAction_Description");
    public static string ToggleSerializedFieldAction_Name => ResourceManager.GetString("ToggleSerializedFieldAction_Name");
    public static string ToggleSerializedFieldAction_Description => ResourceManager.GetString("ToggleSerializedFieldAction_Description");
    public static string FindUnityUsagesText => ResourceManager.GetString("FindUnityUsagesText");
    public static string Unity_Internal_DumpDuplicateTypeNames_Text => ResourceManager.GetString("Unity_Internal_DumpDuplicateTypeNames_Text");
    public static string Unity_Internal_DumpSpellCheckWordLists_Text => ResourceManager.GetString("Unity_Internal_DumpSpellCheckWordLists_Text");
    public static string GoToUnityUsagesProvider_CreateWorkflow_Unity_Usages_of_Symbol => ResourceManager.GetString("GoToUnityUsagesProvider_CreateWorkflow_Unity_Usages_of_Symbol");
    public static string QuickFixRegistrar_Register_Remove_redundant_Unity_event_function => ResourceManager.GetString("QuickFixRegistrar_Register_Remove_redundant_Unity_event_function");
    public static string PackageNotInstalledInfo_Symbol_not_defined__Package___0___is_not_installed => ResourceManager.GetString("PackageNotInstalledInfo_Symbol_not_defined__Package___0___is_not_installed");
    public static string UnmetDefineConstraintInfo_Unmet_define_constraint_0_ => ResourceManager.GetString("UnmetDefineConstraintInfo_Unmet_define_constraint_0_");
    public static string UnmetDefineConstraintInfo____Assembly_definition_will_not_be_compiled => ResourceManager.GetString("UnmetDefineConstraintInfo____Assembly_definition_will_not_be_compiled");
    public static string UnmetVersionConstraintInfo_Symbol_not_defined__Unmet_version_constraint___0_ => ResourceManager.GetString("UnmetVersionConstraintInfo_Symbol_not_defined__Unmet_version_constraint___0_");
    public static string UnityPerformanceCriticalCodeLineMarker_Performance_critical_context => ResourceManager.GetString("UnityPerformanceCriticalCodeLineMarker_Performance_critical_context");
  }
}