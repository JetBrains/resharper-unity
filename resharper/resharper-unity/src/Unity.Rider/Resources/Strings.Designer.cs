namespace JetBrains.ReSharper.Plugins.Unity.Rider.Resources
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
    private static readonly ILogger ourLog = Logger.GetLogger("JetBrains.ReSharper.Plugins.Unity.Rider.Resources.Strings");

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
                  .CreateResourceManager("JetBrains.ReSharper.Plugins.Unity.Rider.Resources.Strings", typeof(Strings).Assembly);
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

    public static string AdditionalFileLayoutSettingsHelper_LoadDefaultPattern_You_are_about_to_replace_the_set_of_patterns_with_a_default_one___0_This_will_remove_all_changes_you_might_have_made__1_Do_you_want_to_proceed_ => ResourceManager.GetString("AdditionalFileLayoutSettingsHelper_LoadDefaultPattern_You_are_about_to_replace_the_set_of_patterns_with_a_default_one___0_This_will_remove_all_changes_you_might_have_made__1_Do_you_want_to_proceed_");
    public static string AdvancedUnityIntegrationIsUnavailable_Text => ResourceManager.GetString("AdvancedUnityIntegrationIsUnavailable_Text");
    public static string ConfigureShaderVariantKeywords_Text => ResourceManager.GetString("ConfigureShaderVariantKeywords_Text");
    public static string DoNotShowForThisSolution_Text => ResourceManager.GetString("DoNotShowForThisSolution_Text");
    public static string GeneratedFileNotification_GeneratedFileNotification_Edit_corresponding__asmdef_in_Unity => ResourceManager.GetString("GeneratedFileNotification_GeneratedFileNotification_Edit_corresponding__asmdef_in_Unity");
    public static string GeneratedFileNotification_GeneratedFileNotification_This_file_is_generated_by_Unity__Any_changes_made_will_be_lost_ => ResourceManager.GetString("GeneratedFileNotification_GeneratedFileNotification_This_file_is_generated_by_Unity__Any_changes_made_will_be_lost_");
    public static string ImmutablePackageNotification_ImmutablePackageNotification_This_file_is_part_of_the_BuildIn_Unity_Package_Cache__Any_changes_made_will_be_lost_ => ResourceManager.GetString("ImmutablePackageNotification_ImmutablePackageNotification_This_file_is_part_of_the_BuildIn_Unity_Package_Cache__Any_changes_made_will_be_lost_");
    public static string ImmutablePackageNotification_ImmutablePackageNotification_This_file_is_part_of_the_Global_Unity_Package_Cache__Any_changes_made_will_be_lost_ => ResourceManager.GetString("ImmutablePackageNotification_ImmutablePackageNotification_This_file_is_part_of_the_Global_Unity_Package_Cache__Any_changes_made_will_be_lost_");
    public static string ImmutablePackageNotification_ImmutablePackageNotification_This_file_is_part_of_the_Unity_Package_Cache__Any_changes_made_will_be_lost_ => ResourceManager.GetString("ImmutablePackageNotification_ImmutablePackageNotification_This_file_is_part_of_the_Unity_Package_Cache__Any_changes_made_will_be_lost_");
    public static string LoadSceneFixBulbAction_Text_Add___SceneName___to_build_settings => ResourceManager.GetString("LoadSceneFixBulbAction_Text_Add___SceneName___to_build_settings");
    public static string MakeSureRider_IsSetAsTheExternalEditor_Text => ResourceManager.GetString("MakeSureRider_IsSetAsTheExternalEditor_Text");
    public static string OpenManifestJson_Text => ResourceManager.GetString("OpenManifestJson_Text");
    public static string PerformanceCriticalLineMarker_RiderPresentableName => ResourceManager.GetString("PerformanceCriticalLineMarker_RiderPresentableName");
    public static string PleaseSwitchToTheUnityEditorToReload_Text => ResourceManager.GetString("PleaseSwitchToTheUnityEditorToReload_Text");
    public static string PleaseSwitchToUnityEditorToLoadThePlugin_Text => ResourceManager.GetString("PleaseSwitchToUnityEditorToLoadThePlugin_Text");
    public static string RiderEventHandlerDetector_AddEventsHighlighting_Assets_usages => ResourceManager.GetString("RiderEventHandlerDetector_AddEventsHighlighting_Assets_usages");
    public static string RiderEventHandlerDetector_AddEventsHighlighting_Click_to_view_usages_in_assets => ResourceManager.GetString("RiderEventHandlerDetector_AddEventsHighlighting_Click_to_view_usages_in_assets");
    public static string RiderFieldDetector_AddMonoBehaviourHighlighting_Inspector_values_are_not_available_during_asset_indexing => ResourceManager.GetString("RiderFieldDetector_AddMonoBehaviourHighlighting_Inspector_values_are_not_available_during_asset_indexing");
    public static string RiderIconProviderUtil_GetExtraActions_Start_Unity_Editor => ResourceManager.GetString("RiderIconProviderUtil_GetExtraActions_Start_Unity_Editor");
    public static string RiderPackageUpdateAvailabilityChecker_ShowNotificationIfNeeded_Check_for_JetBrains_Rider_package__Version__in_Unity_Package_Manager_ => ResourceManager.GetString("RiderPackageUpdateAvailabilityChecker_ShowNotificationIfNeeded_Check_for_JetBrains_Rider_package__Version__in_Unity_Package_Manager_");
    public static string RiderPackageUpdateAvailabilityChecker_ShowNotificationIfNeeded_Do_not_show_for_this_solution => ResourceManager.GetString("RiderPackageUpdateAvailabilityChecker_ShowNotificationIfNeeded_Do_not_show_for_this_solution");
    public static string RiderPackageUpdateAvailabilityChecker_ShowNotificationIfNeeded_Update_available___JetBrains_Rider_package_ => ResourceManager.GetString("RiderPackageUpdateAvailabilityChecker_ShowNotificationIfNeeded_Update_available___JetBrains_Rider_package_");
    public static string RiderTypeDetector_AddMonoBehaviourHighlighting_Usages_in_assets_are_not_available_during_asset_indexing => ResourceManager.GetString("RiderTypeDetector_AddMonoBehaviourHighlighting_Usages_in_assets_are_not_available_during_asset_indexing");
    public static string RiderTypeDetector_AddScriptUsagesHighlighting_Assets_usages => ResourceManager.GetString("RiderTypeDetector_AddScriptUsagesHighlighting_Assets_usages");
    public static string RiderTypeDetector_AddScriptUsagesHighlighting_Click_to_view_usages_in_assets => ResourceManager.GetString("RiderTypeDetector_AddScriptUsagesHighlighting_Click_to_view_usages_in_assets");
    public static string RiderUnityAssetOccurrenceNavigator_Navigate_Start_the_Unity_Editor_to_view_results => ResourceManager.GetString("RiderUnityAssetOccurrenceNavigator_Navigate_Start_the_Unity_Editor_to_view_results");
    public static string SceneManagerLoadSceneEnableQuickFix_Text_Enable_scene_in_build_settings => ResourceManager.GetString("SceneManagerLoadSceneEnableQuickFix_Text_Enable_scene_in_build_settings");
    public static string TheUnityEditorPluginIsOutOfDateAndAutomatic_Text => ResourceManager.GetString("TheUnityEditorPluginIsOutOfDateAndAutomatic_Text");
    public static string ThisBranchMayBeActiveInOneOfShaderVariants_Text => ResourceManager.GetString("ThisBranchMayBeActiveInOneOfShaderVariants_Text");
    public static string UnityAssetsUsage_Text => ResourceManager.GetString("UnityAssetsUsage_Text");
    public static string UnityCodeInsightFieldUsageProvider_DisplayName_Unity_serialized_field => ResourceManager.GetString("UnityCodeInsightFieldUsageProvider_DisplayName_Unity_serialized_field");
    public static string UnityController_StartUnityInternal_Start_Unity_Editor => ResourceManager.GetString("UnityController_StartUnityInternal_Start_Unity_Editor");
    public static string UnityEditorFindUsageResultCreator_CreateRequestToUnity_Finding_usages_in_Unity_for__ => ResourceManager.GetString("UnityEditorFindUsageResultCreator_CreateRequestToUnity_Finding_usages_in_Unity_for__");
    public static string UnityEditorPluginInstalled_Text => ResourceManager.GetString("UnityEditorPluginInstalled_Text");
    public static string UnityEditorPluginUpdated_Text => ResourceManager.GetString("UnityEditorPluginUpdated_Text");
    public static string UnityEditorPluginUpdatedDebugBuild_Text => ResourceManager.GetString("UnityEditorPluginUpdatedDebugBuild_Text");
    public static string UnityEditorPluginUpdatedUpToDate_Text => ResourceManager.GetString("UnityEditorPluginUpdatedUpToDate_Text");
    public static string UnityEditorPluginUpdateRequired_Text => ResourceManager.GetString("UnityEditorPluginUpdateRequired_Text");
    public static string UnityFileLayoutPageTab_Create_Default => ResourceManager.GetString("UnityFileLayoutPageTab_Create_Default");
    public static string UnityFileLayoutPageTab_Create_Default_with_regions => ResourceManager.GetString("UnityFileLayoutPageTab_Create_Default_with_regions");
    public static string UnityFileLayoutPageTab_Create_Empty => ResourceManager.GetString("UnityFileLayoutPageTab_Create_Empty");
    public static string UnityFileLayoutPageTab_Create_Load_patterns_ => ResourceManager.GetString("UnityFileLayoutPageTab_Create_Load_patterns_");
    public static string UnityImplicitUsage_Text => ResourceManager.GetString("UnityImplicitUsage_Text");
    public static string UnityIsNotRunning_Text => ResourceManager.GetString("UnityIsNotRunning_Text");
    public static string UnityPathTemplateParameter_CreateContent_Custom => ResourceManager.GetString("UnityPathTemplateParameter_CreateContent_Custom");
    public static string UnityPathTemplateParameter_CreateContent_Custom__Unity_installation_was_not_found_ => ResourceManager.GetString("UnityPathTemplateParameter_CreateContent_Custom__Unity_installation_was_not_found_");
    public static string UnityPathTemplateParameter_CreateContent_Custom_path => ResourceManager.GetString("UnityPathTemplateParameter_CreateContent_Custom_path");
    public static string UnityRefresher_RefreshInternal_Refreshing_solution_in_Unity_Editor___ => ResourceManager.GetString("UnityRefresher_RefreshInternal_Refreshing_solution_in_Unity_Editor___");
    public static string UnitySettingsCategoryProvider_myCategoryToKeys_Unity_plugin_settings => ResourceManager.GetString("UnitySettingsCategoryProvider_myCategoryToKeys_Unity_plugin_settings");
    public static string UnityStaticMethodRunMarkerGutterMark_GetRunMethodItems_Make_sure_Unity_is_running_ => ResourceManager.GetString("UnityStaticMethodRunMarkerGutterMark_GetRunMethodItems_Make_sure_Unity_is_running_");
    public static string UnityStaticMethodRunMarkerGutterMark_GetRunMethodItems_No_connection_to_Unity => ResourceManager.GetString("UnityStaticMethodRunMarkerGutterMark_GetRunMethodItems_No_connection_to_Unity");
    public static string UnityUsagesCodeVisionProvider_DisplayName_Unity_assets_usage => ResourceManager.GetString("UnityUsagesCodeVisionProvider_DisplayName_Unity_assets_usage");
    public static string UnityUsagesCodeVisionProvider_GetText_No_asset_usages => ResourceManager.GetString("UnityUsagesCodeVisionProvider_GetText_No_asset_usages");
    public static string UnityUsagesCodeVisionProvider_Noun_asset_usage => ResourceManager.GetString("UnityUsagesCodeVisionProvider_Noun_asset_usage");
    public static string UnityUsagesCodeVisionProvider_Noun_asset_usages => ResourceManager.GetString("UnityUsagesCodeVisionProvider_Noun_asset_usages");
    public static string UsagesInAssetsAreNotAvailableDuring_Text => ResourceManager.GetString("UsagesInAssetsAreNotAvailableDuring_Text");
    public static string UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_Changed_in__0___assets => ResourceManager.GetString("UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_Changed_in__0___assets");
    public static string UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_Unchanged => ResourceManager.GetString("UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_Unchanged");
    public static string UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_asset => ResourceManager.GetString("UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_asset");
    public static string UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_assets => ResourceManager.GetString("UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_assets");
    public static string UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_Changed_in__0___1_ => ResourceManager.GetString("UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_Changed_in__0___1_");
    public static string UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_Property_Inspector_values => ResourceManager.GetString("UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_Property_Inspector_values");
    public static string UnityCodeInsightFieldUsageProvider_GetTooltip_Unique_change => ResourceManager.GetString("UnityCodeInsightFieldUsageProvider_GetTooltip_Unique_change");
    public static string UnityCodeInsightFieldUsageProvider_GetTooltip_No_changes_in_assets => ResourceManager.GetString("UnityCodeInsightFieldUsageProvider_GetTooltip_No_changes_in_assets");
    public static string UnityCodeInsightFieldUsageProvider_GetTooltip_Possible_indirect_changes => ResourceManager.GetString("UnityCodeInsightFieldUsageProvider_GetTooltip_Possible_indirect_changes");
    public static string UnityCodeInsightFieldUsageProvider_GetTooltip_Changed_in_1_asset___possible_indirect_changes => ResourceManager.GetString("UnityCodeInsightFieldUsageProvider_GetTooltip_Changed_in_1_asset___possible_indirect_changes");
    public static string UnityCodeInsightFieldUsageProvider_GetTooltip_Changed_in__0__assets___possible_indirect_changes => ResourceManager.GetString("UnityCodeInsightFieldUsageProvider_GetTooltip_Changed_in__0__assets___possible_indirect_changes");
    public static string UnityCodeInsightFieldUsageProvider_GetTooltip_Changed_in__0__assets => ResourceManager.GetString("UnityCodeInsightFieldUsageProvider_GetTooltip_Changed_in__0__assets");
    public static string UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_No_methods => ResourceManager.GetString("UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_No_methods");
    public static string UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_method => ResourceManager.GetString("UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_method");
    public static string UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_CapitalChar_Method => ResourceManager.GetString("UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_CapitalChar_Method");
    public static string UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_methods => ResourceManager.GetString("UnityCodeInsightFieldUsageProvider_AddInspectorHighlighting_methods");
    public static string AnimatorGroupingRule_AnimatorGroupingRule_Animator => ResourceManager.GetString("AnimatorGroupingRule_AnimatorGroupingRule_Animator");
    public static string AnimationEventGroupingRule_AnimationEventGroupingRule_AnimationEvent => ResourceManager.GetString("AnimationEventGroupingRule_AnimationEventGroupingRule_AnimationEvent");
    public static string GameObjectUsageGroupingRule_GameObjectUsageGroupingRule_UnityGameObject => ResourceManager.GetString("GameObjectUsageGroupingRule_GameObjectUsageGroupingRule_UnityGameObject");
    public static string ComponentUsageGroupingRule_ComponentUsageGroupingRule_UnityComponent => ResourceManager.GetString("ComponentUsageGroupingRule_ComponentUsageGroupingRule_UnityComponent");
    public static string ConfigureShaderVariantKeywordsQuickFix_Text => ResourceManager.GetString("ConfigureShaderVariantKeywordsQuickFix_Text");
    public static string InactiveShaderVariantBranch_Text => ResourceManager.GetString("InactiveShaderVariantBranch_Text");
  }
}