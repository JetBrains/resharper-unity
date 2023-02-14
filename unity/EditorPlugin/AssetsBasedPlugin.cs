using System;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Rider.Model.Unity;
using JetBrains.Rider.Unity.Editor.Utils;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor
{
  // Plugin entry point code for the legacy plugin loaded from assets, rather than loaded from the Rider package. The
  // steps done here are already done in the package, or are no longer necessary since 2019.2, when then package was
  // introduced
  internal static class AssetsBasedPlugin
  {
    public static void Initialise(Lifetime lifetime, RiderPathProvider riderPathProvider,
                                  IPluginSettings pluginSettings, ILog logger)
    {
      if (IsLoadedFromAssets())
      {
        UpdateExternalScriptEditor(riderPathProvider, pluginSettings);
        InitialiseOncePerSession(lifetime, logger);
      }
    }

    private static bool IsLoadedFromAssets()
    {
      var currentDir = Directory.GetCurrentDirectory();
      var location = Assembly.GetExecutingAssembly().Location;
      return location.StartsWith(currentDir, StringComparison.InvariantCultureIgnoreCase);
    }

    private static void UpdateExternalScriptEditor(RiderPathProvider riderPathProvider, IPluginSettings pluginSettings)
    {
      // Find the instance of Rider that is the currently selected external editor. If Rider isn't the currently
      // selected external editor, or the currently selected path doesn't exist, return the first Rider found on the
      // system.
      // If there are multiple Rider installs, the sort order is undefined/implementation specific (system install,
      // followed by Toolbox, in order of channel directories read from disk)
      var allPossibleRiderPaths = RiderPathLocator.GetAllFoundPaths(pluginSettings.OperatingSystemFamilyRider);
      var riderPath = riderPathProvider.GetActualRider(EditorPrefsWrapper.ExternalScriptEditor,
        allPossibleRiderPaths);
      if (!string.IsNullOrEmpty(riderPath))
      {
        // Add the found Rider to the list of recently used external editors, so it's visible in the drop down. Note
        // that this only works on Windows - the Unity editor on Mac has a bug and fails to read this setting correctly.
        AddRiderToRecentlyUsedScriptApp(riderPath);

        // If Rider is the currently selected external editor and we want to always use the latest Rider from Toolbox,
        // update the path. This is most likely to be the latest Toolbox version from ch-0
        if (PluginEntryPoint.IsRiderDefaultEditor() && PluginSettings.UseLatestRiderFromToolbox)
        {
          EditorPrefsWrapper.ExternalScriptEditor = riderPath;
        }
      }

      // If this is the first time the plugin is being initialised, set the external editor to Rider. This is persisted
      // so only done once
      if (!PluginSettings.RiderInitializedOnce)
      {
        EditorPrefsWrapper.ExternalScriptEditor = riderPath;
        PluginSettings.RiderInitializedOnce = true;
      }
    }

    private static void AddRiderToRecentlyUsedScriptApp(string userAppPath)
    {
      const string recentAppsKey = "RecentlyUsedScriptApp";

      for (var i = 0; i < 10; ++i)
      {
        var path = EditorPrefs.GetString($"{recentAppsKey}{i}");
        if (File.Exists(path) && Path.GetFileName(path).ToLower().Contains("rider"))
          return;
      }

      EditorPrefs.SetString($"{recentAppsKey}{9}", userAppPath);
    }

    private static void InitialiseOncePerSession(Lifetime lifetime, ILog logger)
    {
      ResetDefaultFileExtensions();

      void SyncSolutionOnceCallBack()
      {
        logger.Verbose("Call SyncSolution once per Unity process.");
        UnityUtils.SyncSolution();
        RiderScriptableSingleton.Instance.CsprojProcessedOnce = true;
        EditorApplication.update -= SyncSolutionOnceCallBack;
      }

      // process csproj files once per Unity process
      if (!RiderScriptableSingleton.Instance.CsprojProcessedOnce)
      {
        // Perform on next editor frame update, so we avoid this exception:
        // "Must set an output directory through SetCompileScriptsOutputDirectory before compiling"
        EditorApplication.update += SyncSolutionOnceCallBack;
      }

      SetupScriptCompilationDuringPlayEvents(lifetime, logger);
    }

    // Unity 2017.3 added "asmdef" to the default list of file extensions used to generate the C# projects, but only for
    // new projects. Existing projects have this value serialised, and Unity doesn't update or reset it. We need .asmdef
    // files in the project, so we'll add it if it's missing.
    // For the record, the default list of file extensions in Unity 2017.4.6f1 is: txt;xml;fnt;cd;asmdef;rsp
    private static void ResetDefaultFileExtensions()
    {
      // ReSharper disable once JoinDeclarationAndInitializer
      string[] currentValues;

      // EditorSettings.projectGenerationUserExtensions (and projectGenerationBuiltinExtensions) were added in 5.2
#if UNITY_5_6_OR_NEWER
      currentValues = EditorSettings.projectGenerationUserExtensions;
#else
      var propertyInfo = typeof(EditorSettings)
        .GetProperty("projectGenerationUserExtensions", BindingFlags.Public | BindingFlags.Static);
      currentValues = propertyInfo?.GetValue(null, null) as string[];
#endif

      if (currentValues != null && !currentValues.Contains("asmdef"))
      {
        var newValues = new string[currentValues.Length + 1];
        Array.Copy(currentValues, newValues, currentValues.Length);
        newValues[currentValues.Length] = "asmdef";

#if UNITY_5_6_OR_NEWER
        EditorSettings.projectGenerationUserExtensions = newValues;
#else
        propertyInfo.SetValue(null, newValues, null);
#endif
      }
    }

    private static void SetupScriptCompilationDuringPlayEvents(Lifetime lifetime, ILog logger)
    {
      // Unity supports recompile/reload settings natively for Unity 2018.2+
      if (UnityUtils.UnityVersion >= new Version(2018, 2))
        return;

      PlayModeStateTracker.Current.Advise(lifetime, state =>
      {
        if (PluginSettings.AssemblyReloadSettings == ScriptCompilationDuringPlay.RecompileAfterFinishedPlaying)
        {
          MainThreadDispatcher.AssertThread();

          if (state == PlayModeState.Playing)
          {
            logger.Info("LockReloadAssemblies");
            EditorApplication.LockReloadAssemblies();
          }
          else if (state == PlayModeState.Stopped)
          {
            logger.Info("UnlockReloadAssemblies");
            EditorApplication.UnlockReloadAssemblies();
          }
        }
      });

      lifetime.OnTermination(() =>
      {
        // Make sure the assemblies are unlocked during AppDomain unload
        if (PluginSettings.AssemblyReloadSettings == ScriptCompilationDuringPlay.StopPlayingAndRecompile &&
            EditorApplication.isPlaying)
        {
          EditorApplication.isPlaying = false;
        }
      });
    }
  }
}