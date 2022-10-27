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
    public static string RiderPackageUpdateAvailabilityChecker_ShowNotificationIfNeeded_Check_for_JetBrains_Rider_package__0__in_Unity_Package_Manager_ => ResourceManager.GetString("RiderPackageUpdateAvailabilityChecker_ShowNotificationIfNeeded_Check_for_JetBrains_Rider_package__0__in_Unity_Package_Manager_");
  }
}