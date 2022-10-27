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
  }
}