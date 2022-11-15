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

    public static string UnityEditorPluginUpdatedUpToDate_Text => ResourceManager.GetString("UnityEditorPluginUpdatedUpToDate_Text");
    public static string PleaseSwitchToTheUnityEditorToReload_Text => ResourceManager.GetString("PleaseSwitchToTheUnityEditorToReload_Text");
    public static string UnityEditorPluginUpdatedDebugBuild_Text => ResourceManager.GetString("UnityEditorPluginUpdatedDebugBuild_Text");
    public static string UnityEditorPluginUpdated_Text => ResourceManager.GetString("UnityEditorPluginUpdated_Text");
    public static string UnityEditorPluginInstalled_Text => ResourceManager.GetString("UnityEditorPluginInstalled_Text");
    public static string PleaseSwitchToUnityEditorToLoadThePlugin_Text => ResourceManager.GetString("PleaseSwitchToUnityEditorToLoadThePlugin_Text");
    public static string UnityEditorPluginUpdateRequired_Text => ResourceManager.GetString("UnityEditorPluginUpdateRequired_Text");
    public static string TheUnityEditorPluginIsOutOfDateAndAutomatic_Text => ResourceManager.GetString("TheUnityEditorPluginIsOutOfDateAndAutomatic_Text");
    public static string DoNotShowForThisSolution_Text => ResourceManager.GetString("DoNotShowForThisSolution_Text");
    public static string AdvancedUnityIntegrationIsUnavailable_Text => ResourceManager.GetString("AdvancedUnityIntegrationIsUnavailable_Text");
    public static string MakeSureRider_IsSetAsTheExternalEditor_Text => ResourceManager.GetString("MakeSureRider_IsSetAsTheExternalEditor_Text");
    public static string RiderPackageUpdateAvailabilityChecker_ShowNotificationIfNeeded_Do_not_show_for_this_solution => ResourceManager.GetString("RiderPackageUpdateAvailabilityChecker_ShowNotificationIfNeeded_Do_not_show_for_this_solution");
    public static string RiderPackageUpdateAvailabilityChecker_ShowNotificationIfNeeded_Update_available___JetBrains_Rider_package_ => ResourceManager.GetString("RiderPackageUpdateAvailabilityChecker_ShowNotificationIfNeeded_Update_available___JetBrains_Rider_package_");
    public static string RiderPackageUpdateAvailabilityChecker_ShowNotificationIfNeeded_Check_for_JetBrains_Rider_package__Version__in_Unity_Package_Manager_ => ResourceManager.GetString("RiderPackageUpdateAvailabilityChecker_ShowNotificationIfNeeded_Check_for_JetBrains_Rider_package__Version__in_Unity_Package_Manager_");
    public static string UsagesInAssetsAreNotAvailableDuring_Text => ResourceManager.GetString("UsagesInAssetsAreNotAvailableDuring_Text");
    public static string UnityImplicitUsage_Text => ResourceManager.GetString("UnityImplicitUsage_Text");
    public static string UnityCodeInsightFieldUsageProvider_DisplayName_Unity_serialized_field => ResourceManager.GetString("UnityCodeInsightFieldUsageProvider_DisplayName_Unity_serialized_field");
    public static string RiderEventHandlerDetector_AddEventsHighlighting_Click_to_view_usages_in_assets => ResourceManager.GetString("RiderEventHandlerDetector_AddEventsHighlighting_Click_to_view_usages_in_assets");
    public static string RiderEventHandlerDetector_AddEventsHighlighting_Assets_usages => ResourceManager.GetString("RiderEventHandlerDetector_AddEventsHighlighting_Assets_usages");
    public static string RiderFieldDetector_AddMonoBehaviourHighlighting_Inspector_values_are_not_available_during_asset_indexing => ResourceManager.GetString("RiderFieldDetector_AddMonoBehaviourHighlighting_Inspector_values_are_not_available_during_asset_indexing");
    public static string UnityIsNotRunning_Text => ResourceManager.GetString("UnityIsNotRunning_Text");
    public static string RiderIconProviderUtil_GetExtraActions_Start_Unity_Editor => ResourceManager.GetString("RiderIconProviderUtil_GetExtraActions_Start_Unity_Editor");
    public static string RiderTypeDetector_AddMonoBehaviourHighlighting_Usages_in_assets_are_not_available_during_asset_indexing => ResourceManager.GetString("RiderTypeDetector_AddMonoBehaviourHighlighting_Usages_in_assets_are_not_available_during_asset_indexing");
    public static string GeneratedFileNotification_GeneratedFileNotification_Edit_corresponding__asmdef_in_Unity => ResourceManager.GetString("GeneratedFileNotification_GeneratedFileNotification_Edit_corresponding__asmdef_in_Unity");
    public static string GeneratedFileNotification_GeneratedFileNotification_This_file_is_generated_by_Unity__Any_changes_made_will_be_lost_ => ResourceManager.GetString("GeneratedFileNotification_GeneratedFileNotification_This_file_is_generated_by_Unity__Any_changes_made_will_be_lost_");
    public static string RiderTypeDetector_AddScriptUsagesHighlighting_Click_to_view_usages_in_assets => ResourceManager.GetString("RiderTypeDetector_AddScriptUsagesHighlighting_Click_to_view_usages_in_assets");
    public static string RiderTypeDetector_AddScriptUsagesHighlighting_Assets_usages => ResourceManager.GetString("RiderTypeDetector_AddScriptUsagesHighlighting_Assets_usages");
    public static string RiderDeferredCacheProgressBar_Start_Processing_assets => ResourceManager.GetString("RiderDeferredCacheProgressBar_Start_Processing_assets");
    public static string RiderDeferredCacheProgressBar_Start_Calculating_asset_index => ResourceManager.GetString("RiderDeferredCacheProgressBar_Start_Calculating_asset_index");
    public static string RiderDeferredCacheProgressBar_Start_Processing_FileName => ResourceManager.GetString("RiderDeferredCacheProgressBar_Start_Processing_FileName");
    public static string UnityPathTemplateParameter_CreateContent_Custom => ResourceManager.GetString("UnityPathTemplateParameter_CreateContent_Custom");
    public static string UnityPathTemplateParameter_CreateContent_Custom__Unity_installation_was_not_found_ => ResourceManager.GetString("UnityPathTemplateParameter_CreateContent_Custom__Unity_installation_was_not_found_");
    public static string UnityPathTemplateParameter_CreateContent_Custom_path => ResourceManager.GetString("UnityPathTemplateParameter_CreateContent_Custom_path");
    public static string UnitySettingsCategoryProvider_myCategoryToKeys_Unity_plugin_settings => ResourceManager.GetString("UnitySettingsCategoryProvider_myCategoryToKeys_Unity_plugin_settings");
    public static string UnityController_StartUnityInternal_Start_Unity_Editor => ResourceManager.GetString("UnityController_StartUnityInternal_Start_Unity_Editor");
    public static string UnityStaticMethodRunMarkerGutterMark_GetRunMethodItems_No_connection_to_Unity => ResourceManager.GetString("UnityStaticMethodRunMarkerGutterMark_GetRunMethodItems_No_connection_to_Unity");
    public static string UnityStaticMethodRunMarkerGutterMark_GetRunMethodItems_Make_sure_Unity_is_running_ => ResourceManager.GetString("UnityStaticMethodRunMarkerGutterMark_GetRunMethodItems_Make_sure_Unity_is_running_");
    public static string SceneManagerLoadSceneEnableQuickFix_Text_Enable_scene_in_build_settings => ResourceManager.GetString("SceneManagerLoadSceneEnableQuickFix_Text_Enable_scene_in_build_settings");
    public static string LoadSceneFixBulbAction_Text_Add___SceneName___to_build_settings => ResourceManager.GetString("LoadSceneFixBulbAction_Text_Add___SceneName___to_build_settings");
    public static string AdditionalFileLayoutSettingsHelper_LoadDefaultPattern_You_are_about_to_replace_the_set_of_patterns_with_a_default_one___0_This_will_remove_all_changes_you_might_have_made__1_Do_you_want_to_proceed_ => ResourceManager.GetString("AdditionalFileLayoutSettingsHelper_LoadDefaultPattern_You_are_about_to_replace_the_set_of_patterns_with_a_default_one___0_This_will_remove_all_changes_you_might_have_made__1_Do_you_want_to_proceed_");
    public static string UnityFileLayoutPageTab_Create_Empty => ResourceManager.GetString("UnityFileLayoutPageTab_Create_Empty");
    public static string UnityFileLayoutPageTab_Create_Default => ResourceManager.GetString("UnityFileLayoutPageTab_Create_Default");
    public static string UnityFileLayoutPageTab_Create_Default_with_regions => ResourceManager.GetString("UnityFileLayoutPageTab_Create_Default_with_regions");
    public static string UnityFileLayoutPageTab_Create_Load_patterns_ => ResourceManager.GetString("UnityFileLayoutPageTab_Create_Load_patterns_");
    public static string ImmutablePackageNotification_ImmutablePackageNotification_This_file_is_part_of_the_Unity_Package_Cache__Any_changes_made_will_be_lost_ => ResourceManager.GetString("ImmutablePackageNotification_ImmutablePackageNotification_This_file_is_part_of_the_Unity_Package_Cache__Any_changes_made_will_be_lost_");
    public static string ImmutablePackageNotification_ImmutablePackageNotification_This_file_is_part_of_the_Global_Unity_Package_Cache__Any_changes_made_will_be_lost_ => ResourceManager.GetString("ImmutablePackageNotification_ImmutablePackageNotification_This_file_is_part_of_the_Global_Unity_Package_Cache__Any_changes_made_will_be_lost_");
    public static string ImmutablePackageNotification_ImmutablePackageNotification_This_file_is_part_of_the_BuildIn_Unity_Package_Cache__Any_changes_made_will_be_lost_ => ResourceManager.GetString("ImmutablePackageNotification_ImmutablePackageNotification_This_file_is_part_of_the_BuildIn_Unity_Package_Cache__Any_changes_made_will_be_lost_");
    public static string UnityRefresher_RefreshInternal_Refreshing_solution_in_Unity_Editor___ => ResourceManager.GetString("UnityRefresher_RefreshInternal_Refreshing_solution_in_Unity_Editor___");
    public static string RiderUnityAssetOccurrenceNavigator_Navigate_Start_the_Unity_Editor_to_view_results => ResourceManager.GetString("RiderUnityAssetOccurrenceNavigator_Navigate_Start_the_Unity_Editor_to_view_results");
    public static string UnityEditorFindUsageResultCreator_CreateRequestToUnity_Finding_usages_in_Unity_for__ => ResourceManager.GetString("UnityEditorFindUsageResultCreator_CreateRequestToUnity_Finding_usages_in_Unity_for__");
  }
}