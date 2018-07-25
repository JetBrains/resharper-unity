using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework;
using JetBrains.Platform.RdFramework.Base;
using JetBrains.Platform.RdFramework.Impl;
using JetBrains.Platform.RdFramework.Tasks;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.Rider.Unity.Editor.AssetPostprocessors;
using JetBrains.Rider.Unity.Editor.NonUnity;
using JetBrains.Rider.Unity.Editor.Utils;
using JetBrains.Util;
using JetBrains.Util.Logging;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor
{
  [InitializeOnLoad]
  public static class PluginEntryPoint
  {
    private static readonly ILog ourLogger = Log.GetLog("RiderPlugin");

    private static readonly LifetimeDefinition ourAppDomainLifetimeDefinition;
    private static readonly UnityEventCollector ourLogEventCollector;
    private static readonly IPluginSettings ourPluginSettings;
    private static readonly RiderPathLocator ourRiderPathLocator;
    private static bool ourInitialized;
    private static OnOpenAssetHandler ourOpenAssetHandler;

    internal static string SlnFile;

    public delegate void OnModelInitializationHandler(ModelWithLifetime e);

    [UsedImplicitly]
    public static event OnModelInitializationHandler OnModelInitialization = delegate {};
    public static readonly List<ModelWithLifetime> UnityModels = new List<ModelWithLifetime>();

    // This an entry point
    static PluginEntryPoint()
    {
      RiderLoggerFactory.Init();

      ourLogger.Trace("Rider::PluginEntryPoint");

      ourAppDomainLifetimeDefinition = CreateAppDomainLifetimeDefinition();

      // Start collecting log events as soon as we can, so we miss as few as possible
      ourLogEventCollector = new UnityEventCollector();
      ourLogEventCollector.StartCollecting(ourAppDomainLifetimeDefinition.Lifetime);

      ourPluginSettings = new PluginSettings();
      ourRiderPathLocator = new RiderPathLocator(ourPluginSettings);
      var riderPath = ourRiderPathLocator.GetDefaultRiderApp(EditorPrefsWrapper.ExternalScriptEditor);
      if (string.IsNullOrEmpty(riderPath))
      {
        ourLogger.Warn("Cannot find installed Rider! Aborting");
        return;
      }

      AddRiderToRecentlyUsedScriptApp(riderPath);
      SetDefaultApp(riderPath);

      if (Enabled)
      {
        Init(ourAppDomainLifetimeDefinition.Lifetime);
      }
    }

    public static bool CallRider(string args)
    {
      return ourOpenAssetHandler != null && ourOpenAssetHandler.CallRider(args);
    }

    public static bool Enabled
    {
      get
      {
        var defaultApp = EditorPrefsWrapper.ExternalScriptEditor;
        return !string.IsNullOrEmpty(defaultApp) && Path.GetFileName(defaultApp).ToLower().Contains("rider");
      }
    }

    [OnOpenAsset]
    private static bool OnOpenedAsset(int instanceID, int line)
    {
      if (!Enabled)
        return false;

      if (!ourInitialized)
      {
        // make sure the plugin was initialized first.
        // this can happen in case "Rider" was set as the default scripting app only after this plugin was imported.
        Init(ourAppDomainLifetimeDefinition.Lifetime);
      }

      return ourOpenAssetHandler.OnOpenedAsset(instanceID, line);
    }

    private static LifetimeDefinition CreateAppDomainLifetimeDefinition()
    {
      var appDomainLifetimeDefinition = Lifetimes.Define(EternalLifetime.Instance);
      AppDomain.CurrentDomain.DomainUnload += (EventHandler) ((_, __) =>
      {
        ourLogger.Verbose("appDomainLifetimeDefinition.Terminate");
        appDomainLifetimeDefinition.Terminate();
      });

      return appDomainLifetimeDefinition;
    }

    // So that Rider appears in the drop down of known external editors
    private static void AddRiderToRecentlyUsedScriptApp(string userAppPath)
    {
      const string recentAppsKey = "RecentlyUsedScriptApp";

      for (var i = 0; i < 10; ++i)
      {
        var path = EditorPrefs.GetString($"{recentAppsKey}{i}");
        // ReSharper disable once PossibleNullReferenceException
        if (File.Exists(path) && Path.GetFileName(path).ToLower().Contains("rider"))
          return;
      }

      EditorPrefs.SetString($"{recentAppsKey}{9}", userAppPath);
    }

    private static void SetDefaultApp(string riderPath)
    {
      // Only set Rider as the default app the very first time the plugin is installed/initialised. After that, respect
      // the user's preferences
      if (!PluginSettings.RiderInitializedOnce)
      {
        ourLogger.Verbose("Setting Rider as default external editor");

        EditorPrefsWrapper.ExternalScriptEditor = riderPath;
        PluginSettings.RiderInitializedOnce = true;
      }
      else
      {
        ourLogger.Trace("Not setting Rider as default external editor");
      }
    }

    private static void Init(Lifetime appDomainLifetime)
    {
      var projectDirectory = Directory.GetParent(Application.dataPath).FullName;
      var projectName = Path.GetFileName(projectDirectory);
      SlnFile = Path.GetFullPath($"{projectName}.sln");

      InitializeEditorInstanceJson(appDomainLifetime);
      ResetDefaultFileExtensions();

      // process csproj files once per Unity process
      if (!RiderScriptableSingleton.Instance.CsprojProcessedOnce)
      {
        ourLogger.Verbose("Call OnGeneratedCSProjectFiles once per Unity process.");
        CsprojAssetPostprocessor.OnGeneratedCSProjectFiles();
        RiderScriptableSingleton.Instance.CsprojProcessedOnce = true;
      }

      if (PluginSettings.SelectedLoggingLevel >= LoggingLevel.VERBOSE)
        Debug.Log($"Rider plugin initialized. LoggingLevel: {PluginSettings.SelectedLoggingLevel}. Change it in Unity Preferences -> Rider. Logs path: {RiderLogger.LogPath}.");

      var list = new List<ProtocolInstance>();
      CreateProtocolAndAdvise(appDomainLifetime, list, new DirectoryInfo(Directory.GetCurrentDirectory()).Name);

      // list all sln files in CurrentDirectory, except main one and create server protocol for each of them
      var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
      var solutionFiles = currentDir.GetFiles("*.sln", SearchOption.TopDirectoryOnly);
      foreach (var solutionFile in solutionFiles)
      {
        if (Path.GetFileNameWithoutExtension(solutionFile.FullName) != currentDir.Name)
        {
          CreateProtocolAndAdvise(appDomainLifetime, list, Path.GetFileNameWithoutExtension(solutionFile.FullName));
        }
      }

      ourLogger.Verbose("Writing Library/ProtocolInstance.json");
      var protocolInstanceJsonPath = Path.GetFullPath("Library/ProtocolInstance.json");
      File.WriteAllText(protocolInstanceJsonPath, ProtocolInstance.ToJson(list));

      appDomainLifetime.AddAction(() =>
      {
        ourLogger.Verbose("Deleting Library/ProtocolInstance.json");
        File.Delete(protocolInstanceJsonPath);
      });

      ourOpenAssetHandler = new OnOpenAssetHandler(ourRiderPathLocator, ourPluginSettings, SlnFile);

      ourSavedState = GetEditorState();
      SetupAssemblyReloadEvents(appDomainLifetime);

      ourInitialized = true;
    }

    // Later versions of Unity also write this file, although it doesn't have all of the same properties
    // TODO: What version added this?
    // TODO: Should we not write it if it already exists?
    private static void InitializeEditorInstanceJson(Lifetime appDomainLifetime)
    {
      ourLogger.Verbose("Writing Library/EditorInstance.json");

      var editorInstanceJsonPath = Path.GetFullPath("Library/EditorInstance.json");

      File.WriteAllText(editorInstanceJsonPath, $@"{{
  ""process_id"": {System.Diagnostics.Process.GetCurrentProcess().Id},
  ""version"": ""{Application.unityVersion}"",
  ""app_path"": ""{EditorApplication.applicationPath}"",
  ""app_contents_path"": ""{EditorApplication.applicationContentsPath}"",
  ""attach_allowed"": ""{EditorPrefs.GetBool("AllowAttachedDebuggingOfEditor", true)}"",
  ""is_loaded_from_assets"": ""{IsLoadedFromAssets()}""
}}");

      appDomainLifetime.AddAction(() =>
      {
        ourLogger.Verbose("Deleting Library/EditorInstance.json");
        File.Delete(editorInstanceJsonPath);
      });
    }

    // Unity 2017.3 added "asmdef" to the default list of file extensions used to generate the C# projects, but only for
    // new projects. Existing projects have this value serialised, and Unity doesn't update or reset it. We need .asmdef
    // files in the project, so we'll add it if it's missing.
    // For the record, the default list of file extensions in Unity 2017.4.6f1 is: txt;xml;fnt;cd;asmdef;rsp
    private static void ResetDefaultFileExtensions()
    {
      // EditorSettings.projectGenerationUserExtensions (and projectGenerationBuiltinExtensions) were added in 5.2. We
      // support 5.0+, so yay! reflection
      var propertyInfo = typeof(EditorSettings)
        .GetProperty("projectGenerationUserExtensions", BindingFlags.Public | BindingFlags.Static);
      if (propertyInfo?.GetValue(null, null) is string[] currentValues)
      {
        if (!currentValues.Contains("asmdef"))
        {
          var newValues = new string[currentValues.Length + 1];
          Array.Copy(currentValues, newValues, currentValues.Length);
          newValues[currentValues.Length] = "asmdef";

          propertyInfo.SetValue(null, newValues, null);
        }
      }
    }

    private static PlayModeState GetEditorState()
    {
      if (EditorApplication.isPaused)
        return PlayModeState.Paused;
      if (EditorApplication.isPlaying)
        return PlayModeState.Playing;
      return PlayModeState.Stopped;
    }

    private static void SetupAssemblyReloadEvents(Lifetime appDomainLifetime)
    {
#pragma warning disable 618
      EditorApplication.playmodeStateChanged += () =>
#pragma warning restore 618
      {
        var newState = GetEditorState();
        if (ourSavedState != newState)
        {
          if (PluginSettings.AssemblyReloadSettings == AssemblyReloadSettings.RecompileAfterFinishedPlaying)
          {
            if (newState == PlayModeState.Playing)
            {
              EditorApplication.LockReloadAssemblies();
            }
            else if (newState == PlayModeState.Stopped)
            {
              EditorApplication.UnlockReloadAssemblies();
            }
          }
          ourSavedState = newState;
        }
      };

      appDomainLifetime.AddAction(() =>
      {
        if (PluginSettings.AssemblyReloadSettings == AssemblyReloadSettings.StopPlayingAndRecompile)
        {
          if (EditorApplication.isPlaying)
          {
            EditorApplication.isPlaying = false;
          }
        }
      });
    }

    private static void CreateProtocolAndAdvise(Lifetime lifetime, List<ProtocolInstance> list, string solutionFileName)
    {
      try
      {
        var riderProtocolController = new RiderProtocolController(MainThreadDispatcher.Instance, lifetime);
        list.Add(new ProtocolInstance(riderProtocolController.Wire.Port, solutionFileName));

        var serializers = new Serializers();
        var identities = new Identities(IdKind.Server);

        MainThreadDispatcher.AssertThread();

        riderProtocolController.Wire.Connected.WhenTrue(lifetime, connectionLifetime =>
        {
          ourLogger.Verbose("Create UnityModel and advise for new sessions...");

          var protocol = new Protocol("UnityEditorPlugin", serializers, identities, MainThreadDispatcher.Instance, riderProtocolController.Wire);
          var model = new EditorPluginModel(connectionLifetime, protocol);
          var modelWithLifetime = new ModelWithLifetime(model, connectionLifetime);

          AdviseUnityActions(model, connectionLifetime);
          AdviseEditorState(model);
          OnModelInitialization(modelWithLifetime);
          AdviseRefresh(model);
          InitEditorLogPath(model);

          model.FullPluginPath.Advise(connectionLifetime, AdditionalPluginsInstaller.UpdateSelf);
          model.ApplicationVersion.SetValue(UnityUtils.UnityVersion.ToString());
          model.ScriptingRuntime.SetValue(UnityUtils.ScriptingRuntime);

          ourLogger.Verbose("UnityModel initialized.");
          connectionLifetime.AddAction(() => { UnityModels.Remove(modelWithLifetime); });
          UnityModels.Add(modelWithLifetime);

          new UnityEventLogSender(ourLogEventCollector);
        });
      }
      catch (Exception ex)
      {
        ourLogger.Error("Init Rider Plugin " + ex);
      }
    }

    private static void AdviseEditorState(EditorPluginModel modelValue)
    {
      modelValue.GetUnityEditorState.Set(rdVoid =>
      {
        if (EditorApplication.isPlaying)
        {
          return UnityEditorState.Play;
        }

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
          return UnityEditorState.Refresh;
        }

        return UnityEditorState.Idle;
      });
    }

    private static void AdviseRefresh(EditorPluginModel model)
    {
      model.Refresh.Set((l, force) =>
      {
        var task = new RdTask<RdVoid>();
        MainThreadDispatcher.Instance.Queue(() =>
        {
          if (!EditorApplication.isPlaying && EditorPrefsWrapper.AutoRefresh || force)
            UnityUtils.SyncSolution();
          else
            ourLogger.Verbose("AutoRefresh is disabled via Unity settings.");
          task.Set(RdVoid.Instance);
        });
        return task;
      });
    }

    public enum PlayModeState
    {
      Stopped,
      Playing,
      Paused
    }

    private static PlayModeState ourSavedState = PlayModeState.Stopped;

    private static void AdviseUnityActions(EditorPluginModel model, Lifetime connectionLifetime)
    {
      var isPlayingAction = new Action(() =>
      {
        MainThreadDispatcher.Instance.Queue(() =>
        {
          var isPlayOrWillChange = EditorApplication.isPlayingOrWillChangePlaymode;
          var isPlaying = isPlayOrWillChange && EditorApplication.isPlaying;
          if (!model.Play.HasValue() || model.Play.HasValue() && model.Play.Value != isPlaying)
            model.Play.SetValue(isPlaying);

          var isPaused = EditorApplication.isPaused;
          model.Pause.SetValue(isPaused);
        });
      });
      isPlayingAction(); // get Unity state
      model.Play.Advise(connectionLifetime, play =>
      {
        MainThreadDispatcher.Instance.Queue(() =>
        {
          var res = EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.isPlaying;
          if (res != play)
            EditorApplication.isPlaying = play;
        });
      });

      model.Pause.Advise(connectionLifetime, pause =>
      {
        MainThreadDispatcher.Instance.Queue(() =>
        {
          EditorApplication.isPaused = pause;
        });
      });

      model.Step.Set((l, x) =>
      {
        var task = new RdTask<RdVoid>();
        MainThreadDispatcher.Instance.Queue(() =>
        {
          EditorApplication.Step();
          task.Set(RdVoid.Instance);
        });
        return task;
      });

      var isPlayingHandler = new EditorApplication.CallbackFunction(() => isPlayingAction());
// left for compatibility with Unity <= 5.5
#pragma warning disable 618
      connectionLifetime.AddBracket(() => { EditorApplication.playmodeStateChanged += isPlayingHandler; },
        () => { EditorApplication.playmodeStateChanged -= isPlayingHandler; });
#pragma warning restore 618
      // new api - not present in Unity 5.5
      // private static Action<PauseState> IsPauseStateChanged(UnityModel model)
      //    {
      //      return state => model?.Pause.SetValue(state == PauseState.Paused);
      //    }
    }

    private static void InitEditorLogPath(EditorPluginModel editorPluginModel)
    {
      // https://docs.unity3d.com/Manual/LogFiles.html
      //PlayerSettings.productName;
      //PlayerSettings.companyName;
      //~/Library/Logs/Unity/Editor.log
      //C:\Users\username\AppData\Local\Unity\Editor\Editor.log
      //~/.config/unity3d/Editor.log

      string editorLogpath = string.Empty;
      string playerLogPath = string.Empty;

      switch (PluginSettings.SystemInfoRiderPlugin.operatingSystemFamily)
      {
        case OperatingSystemFamilyRider.Windows:
        {
          var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
          editorLogpath = Path.Combine(localAppData, @"Unity\Editor\Editor.log");
          var userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
          if (!string.IsNullOrEmpty(userProfile))
            playerLogPath = Path.Combine(
              Path.Combine(Path.Combine(Path.Combine(userProfile, @"AppData\LocalLow"), PlayerSettings.companyName),
                PlayerSettings.productName),"output_log.txt");
          break;
        }
        case OperatingSystemFamilyRider.MacOSX:
        {
          var home = Environment.GetEnvironmentVariable("HOME");
          if (!string.IsNullOrEmpty(home))
          {
            editorLogpath = Path.Combine(home, "Library/Logs/Unity/Editor.log");
            playerLogPath = Path.Combine(home, "Library/Logs/Unity/Player.log");
          }
          break;
        }
        case OperatingSystemFamilyRider.Linux:
        {
          var home = Environment.GetEnvironmentVariable("HOME");
          if (!string.IsNullOrEmpty(home))
          {
            editorLogpath = Path.Combine(home, ".config/unity3d/Editor.log");
            playerLogPath = Path.Combine(home, $".config/unity3d/{PlayerSettings.companyName}/{PlayerSettings.productName}/Player.log");
          }
          break;
        }
      }

      editorPluginModel.EditorLogPath.SetValue(editorLogpath);
      editorPluginModel.PlayerLogPath.SetValue(playerLogPath);
    }

    // TODO: I don't know what this method is for...
    internal static bool IsLoadedFromAssets()
    {
      var currentDir = Directory.GetCurrentDirectory();
      var location = Assembly.GetExecutingAssembly().Location;
      return location.StartsWith(currentDir, StringComparison.InvariantCultureIgnoreCase);
    }
  }
}

// Developed with JetBrains Rider =)
