using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using JetBrains.Collections.Viewable;
using JetBrains.Core;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Rd;
using JetBrains.Rd.Base;
using JetBrains.Rd.Impl;
using JetBrains.Rd.Tasks;
using UnityEditor;
using Application = UnityEngine.Application;
using Debug = UnityEngine.Debug;
using JetBrains.Rider.Unity.Editor.NonUnity;
using JetBrains.Rider.Unity.Editor.Utils;
using UnityEditor.Callbacks;

namespace JetBrains.Rider.Unity.Editor
{
  [InitializeOnLoad]
  public static class PluginEntryPoint
  {
    private static readonly IPluginSettings ourPluginSettings;
    private static readonly RiderPathProvider ourRiderPathProvider;
    public static readonly List<ModelWithLifetime> UnityModels = new List<ModelWithLifetime>();
    private static bool ourInitialized;
    private static readonly ILog ourLogger = Log.GetLog("RiderPlugin");
    internal static string SlnFile;

    // This an entry point
    static PluginEntryPoint()
    {
      if (UnityEditorInternal.InternalEditorUtility.inBatchMode)
        return;

      PluginSettings.InitLog(); // init log before doing any logging
      UnityEventLogSender.Start(); // start collecting Unity messages asap

      ourPluginSettings = new PluginSettings();
      ourRiderPathProvider = new RiderPathProvider(ourPluginSettings);

      if (IsLoadedFromAssets()) // old mechanism, when EditorPlugin was copied to Assets folder
      {
          var riderPath = ourRiderPathProvider.GetActualRider(EditorPrefsWrapper.ExternalScriptEditor,
          RiderPathLocator.GetAllFoundPaths(ourPluginSettings.OperatingSystemFamilyRider));
        if (!string.IsNullOrEmpty(riderPath))
        {
          AddRiderToRecentlyUsedScriptApp(riderPath);
          if (IsRiderDefaultEditor() && PluginSettings.UseLatestRiderFromToolbox)
          {
            EditorPrefsWrapper.ExternalScriptEditor = riderPath;
          }
        }

        if (!PluginSettings.RiderInitializedOnce)
        {
          EditorPrefsWrapper.ExternalScriptEditor = riderPath;
          PluginSettings.RiderInitializedOnce = true;
        }

        InitForPluginLoadedFromAssets();
        Init();
      }
      else
      {
        Init();
      }
    }

    public delegate void OnModelInitializationHandler(UnityModelAndLifetime e);
    [UsedImplicitly]
    public static event OnModelInitializationHandler OnModelInitialization = delegate {};

    internal static bool CheckConnectedToBackendSync(EditorPluginModel model)
    {
      if (model == null)
        return false;
      var connected = false;
      try
      {
        // HostConnected also means that in Rider and in Unity the same solution is opened
        connected = model.IsBackendConnected.Sync(Unit.Instance,
          new RpcTimeouts(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(200)));
      }
      catch (Exception)
      {
        ourLogger.Verbose("Rider Protocol not connected.");
      }

      return connected;
    }

    public static bool CallRider(string args)
    {
      return OpenAssetHandler.CallRider(args);
    }

    public static bool IsRiderDefaultEditor()
    {
        // Regular check
        var defaultApp = EditorPrefsWrapper.ExternalScriptEditor;
        bool isEnabled = !string.IsNullOrEmpty(defaultApp) &&
                         Path.GetFileName(defaultApp).ToLower().Contains("rider") &&
                         !UnityEditorInternal.InternalEditorUtility.inBatchMode;

        return isEnabled;
    }

    public static void Init()
    {
      if (ourInitialized)
        return;

      var projectDirectory = Directory.GetParent(Application.dataPath).FullName;
      var projectName = Path.GetFileName(projectDirectory);
      SlnFile = Path.GetFullPath($"{projectName}.sln");

      InitializeEditorInstanceJson();

      var lifetimeDefinition = Lifetime.Define(Lifetime.Eternal);
      var lifetime = lifetimeDefinition.Lifetime;

      AppDomain.CurrentDomain.DomainUnload += (EventHandler) ((_, __) =>
      {
        ourLogger.Verbose("lifetimeDefinition.Terminate");
        lifetimeDefinition.Terminate();
      });

      if (PluginSettings.SelectedLoggingLevel >= LoggingLevel.VERBOSE)
      {
        var executingAssembly = Assembly.GetExecutingAssembly();
        var location = executingAssembly.Location;
        Debug.Log($"Rider plugin \"{executingAssembly.GetName().Name}\" initialized{(string.IsNullOrEmpty(location)? "" : " from: " + location )}. LoggingLevel: {PluginSettings.SelectedLoggingLevel}. Change it in Unity Preferences -> Rider. Logs path: {LogPath}.");
      }

      var list = new List<ProtocolInstance>();
      CreateProtocolAndAdvise(lifetime, list, new DirectoryInfo(Directory.GetCurrentDirectory()).Name);

      // list all sln files in CurrentDirectory, except main one and create server protocol for each of them
      var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
      var solutionFiles = currentDir.GetFiles("*.sln", SearchOption.TopDirectoryOnly);
      foreach (var solutionFile in solutionFiles)
      {
        if (Path.GetFileNameWithoutExtension(solutionFile.FullName) != currentDir.Name)
        {
          CreateProtocolAndAdvise(lifetime, list, Path.GetFileNameWithoutExtension(solutionFile.FullName));
        }
      }

      OpenAssetHandler = new OnOpenAssetHandler(ourRiderPathProvider, ourPluginSettings, SlnFile);
      ourLogger.Verbose("Writing Library/ProtocolInstance.json");
      var protocolInstanceJsonPath = Path.GetFullPath("Library/ProtocolInstance.json");
      File.WriteAllText(protocolInstanceJsonPath, ProtocolInstance.ToJson(list));

      AppDomain.CurrentDomain.DomainUnload += (sender, args) =>
      {
        ourLogger.Verbose("Deleting Library/ProtocolInstance.json");
        File.Delete(protocolInstanceJsonPath);
      };

      PlayModeSavedState = GetPlayModeState();

      ourInitialized = true;
    }

    internal static void InitForPluginLoadedFromAssets()
    {
      if (ourInitialized)
        return;

      ResetDefaultFileExtensions();

      // process csproj files once per Unity process
      if (!RiderScriptableSingleton.Instance.CsprojProcessedOnce)
      {
        // Perform on next editor frame update, so we avoid this exception:
        // "Must set an output directory through SetCompileScriptsOutputDirectory before compiling"
        EditorApplication.update += SyncSolutionOnceCallBack;
      }

      SetupAssemblyReloadEvents();
    }

    private static void SyncSolutionOnceCallBack()
    {
      ourLogger.Verbose("Call SyncSolution once per Unity process.");
      UnityUtils.SyncSolution();
      RiderScriptableSingleton.Instance.CsprojProcessedOnce = true;
      EditorApplication.update -= SyncSolutionOnceCallBack;
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

    public enum PlayModeState
    {
      Stopped,
      Playing,
      Paused
    }

    public static PlayModeState PlayModeSavedState = PlayModeState.Stopped;

    private static PlayModeState GetPlayModeState()
    {
      if (EditorApplication.isPaused)
        return PlayModeState.Paused;
      if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        return PlayModeState.Playing;
      return PlayModeState.Stopped;
    }

    private static void SetupAssemblyReloadEvents()
    {
      // playmodeStateChanged was marked obsolete in 2017.1. Still working in 2018.3
#pragma warning disable 618
      EditorApplication.playmodeStateChanged += () =>
#pragma warning restore 618
      {
        if (PluginSettings.AssemblyReloadSettings == AssemblyReloadSettings.RecompileAfterFinishedPlaying)
        {
          MainThreadDispatcher.Instance.Queue(() =>
          {
            var newPlayModeState = GetPlayModeState();
            if (PlayModeSavedState != newPlayModeState)
            {
              if (newPlayModeState == PlayModeState.Playing)
              {
                ourLogger.Info("LockReloadAssemblies");
                EditorApplication.LockReloadAssemblies();
              }
              else if (newPlayModeState == PlayModeState.Stopped)
              {
                ourLogger.Info("UnlockReloadAssemblies");
                EditorApplication.UnlockReloadAssemblies();
              }
              PlayModeSavedState = newPlayModeState;
            }
          });
        }
      };

      AppDomain.CurrentDomain.DomainUnload += (sender, args) =>
      {
        if (PluginSettings.AssemblyReloadSettings == AssemblyReloadSettings.StopPlayingAndRecompile)
        {
          if (EditorApplication.isPlaying)
          {
            EditorApplication.isPlaying = false;
          }
        }
      };
    }

    private static void CreateProtocolAndAdvise(Lifetime lifetime, List<ProtocolInstance> list, string solutionName)
    {
      try
      {
        var dispatcher = MainThreadDispatcher.Instance;
        var riderProtocolController = new RiderProtocolController(dispatcher, lifetime);
        list.Add(new ProtocolInstance(riderProtocolController.Wire.Port, solutionName));

#if !NET35
        var serializers = new Serializers(lifetime, null, null);
#else
        var serializers = new Serializers();
#endif
        var identities = new Identities(IdKind.Server);

        MainThreadDispatcher.AssertThread();
        var protocol = new Protocol("UnityEditorPlugin" + solutionName, serializers, identities, MainThreadDispatcher.Instance, riderProtocolController.Wire, lifetime);
        riderProtocolController.Wire.Connected.WhenTrue(lifetime, connectionLifetime =>
        {
          ourLogger.Log(LoggingLevel.VERBOSE, "Create UnityModel and advise for new sessions...");
          var model = new EditorPluginModel(connectionLifetime, protocol);
          AdviseUnityActions(model, connectionLifetime);
          AdviseEditorState(model);
          OnModelInitialization(new UnityModelAndLifetime(model, connectionLifetime));
          AdviseRefresh(model);
          InitEditorLogPath(model);

          model.UnityProcessId.SetValue(Process.GetCurrentProcess().Id);
          model.UnityApplicationData.SetValue(new UnityApplicationData(
            EditorApplication.applicationPath,
            EditorApplication.applicationContentsPath, UnityUtils.UnityApplicationVersion));
          model.ScriptingRuntime.SetValue(UnityUtils.ScriptingRuntime);

          if (UnityUtils.UnityVersion >= new Version(2018, 2))
            model.ScriptCompilationDuringPlay.Set(EditorPrefsWrapper.ScriptChangesDuringPlayOptions);
          else
            model.ScriptCompilationDuringPlay.Set((int)PluginSettings.AssemblyReloadSettings);

          AdviseShowPreferences(model, connectionLifetime, ourLogger);
          AdviseGenerateUISchema(model);
          AdviseExitUnity(model);
          GetBuildLocation(model);

          ourLogger.Verbose("UnityModel initialized.");
          var pair = new ModelWithLifetime(model, connectionLifetime);
          connectionLifetime.OnTermination(() => { UnityModels.Remove(pair); });
          UnityModels.Add(pair);
        });
      }
      catch (Exception ex)
      {
        ourLogger.Error("Init Rider Plugin " + ex);
      }
    }

    private static void GetBuildLocation(EditorPluginModel model)
    {
        var path = EditorUserBuildSettings.GetBuildLocation(EditorUserBuildSettings.selectedStandaloneTarget);
        if (PluginSettings.SystemInfoRiderPlugin.operatingSystemFamily == OperatingSystemFamilyRider.MacOSX)
            path = Path.Combine(Path.Combine(Path.Combine(path, "Contents"), "MacOS"), PlayerSettings.productName);
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
            model.BuildLocation.Value = path;
    }

    private static void AdviseGenerateUISchema(EditorPluginModel model)
    {
      model.GenerateUIElementsSchema.Set(_ => UIElementsSupport.GenerateSchema());
    }

    private static void AdviseExitUnity(EditorPluginModel model)
    {
      model.ExitUnity.Set((_, rdVoid) =>
      {
        var task = new RdTask<bool>();
        MainThreadDispatcher.Instance.Queue(() =>
        {
          try
          {
            ourLogger.Verbose("ExitUnity: Started");
            EditorApplication.Exit(0);
            ourLogger.Verbose("ExitUnity: Completed");
            task.Set(true);
          }
          catch (Exception e)
          {
            ourLogger.Log(LoggingLevel.WARN, "EditorApplication.Exit failed.", e);
            task.Set(false);
          }
        });
        return task;
      });
    }

    private static void AdviseShowPreferences(EditorPluginModel model, Lifetime connectionLifetime, ILog log)
    {
      model.ShowPreferences.Advise(connectionLifetime, result =>
      {
        if (result != null)
        {
          MainThreadDispatcher.Instance.Queue(() =>
          {
            try
            {
              var tab = UnityUtils.UnityVersion >= new Version(2018, 2) ? "_General" : "Rider";

              var type = typeof(SceneView).Assembly.GetType("UnityEditor.SettingsService");
              if (type != null)
              {
                // 2018+
                var method = type.GetMethod("OpenUserPreferences", BindingFlags.Static | BindingFlags.Public);

                if (method == null)
                {
                  log.Error("'OpenUserPreferences' was not found");
                }
                else
                {
                  method.Invoke(null, new object[] {$"Preferences/{tab}"});
                }
              }
              else
              {
                // 5.5, 2017 ...
                type = typeof(SceneView).Assembly.GetType("UnityEditor.PreferencesWindow");
                var method = type?.GetMethod("ShowPreferencesWindow", BindingFlags.Static | BindingFlags.NonPublic);

                if (method == null)
                {
                  log.Error("'ShowPreferencesWindow' was not found");
                }
                else
                {
                  method.Invoke(null, null);
                }
              }
            }
            catch (Exception ex)
            {
              log.Error("Show preferences " + ex);
            }
          });
        }
      });
    }

    private static void AdviseEditorState(EditorPluginModel modelValue)
    {
      modelValue.GetUnityEditorState.Set(rdVoid =>
      {
        if (EditorApplication.isPaused)
        {
          return UnityEditorState.Pause;
        }

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
        var refreshTask = new RdTask<Unit>();
        void SendResult()
        {
          if (!EditorApplication.isCompiling)
          {
            // ReSharper disable once DelegateSubtraction
            EditorApplication.update -= SendResult;
            ourLogger.Verbose("Refresh: SyncSolution Completed");
            refreshTask.Set(Unit.Instance);
          }
        }
        
        ourLogger.Verbose("Refresh: SyncSolution Enqueue");
        MainThreadDispatcher.Instance.Queue(() =>
        {
          if (!EditorApplication.isPlaying && EditorPrefsWrapper.AutoRefresh || force != RefreshType.Normal)
          {
            try
            {
              if (force == RefreshType.ForceRequestScriptReload)
              {
                ourLogger.Verbose("Refresh: RequestScriptReload");
                UnityEditorInternal.InternalEditorUtility.RequestScriptReload();
              }

              ourLogger.Verbose("Refresh: SyncSolution Started");
              UnityUtils.SyncSolution();
            }
            catch (Exception e)
            {
              ourLogger.Error("Refresh failed with exception", e);
            }
            finally
            {
              EditorApplication.update += SendResult;
            }
          }
          else
          {
            refreshTask.Set(Unit.Instance);
            ourLogger.Verbose("AutoRefresh is disabled via Unity settings.");
          }
        });
        return refreshTask;
      });
    }

    private static void AdviseUnityActions(EditorPluginModel model, Lifetime connectionLifetime)
    {
      var syncPlayState = new Action(() =>
      {
        MainThreadDispatcher.Instance.Queue(() =>
        {
          var isPlaying = EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.isPlaying;

          if (!model.Play.HasValue() || model.Play.HasValue() && model.Play.Value != isPlaying)
          {
            ourLogger.Verbose("Reporting play mode change to model: {0}", isPlaying);
            model.Play.SetValue(isPlaying);
            if (isPlaying)
              model.LastPlayTime.SetValue(DateTime.UtcNow.Ticks);
          }

          var isPaused = EditorApplication.isPaused;
          if (!model.Pause.HasValue() || model.Pause.HasValue() && model.Pause.Value != isPaused)
          {
            ourLogger.Verbose("Reporting pause mode change to model: {0}", isPaused);
            model.Pause.SetValue(isPaused);
          }
        });
      });

      syncPlayState();

      model.Play.Advise(connectionLifetime, play =>
      {
        MainThreadDispatcher.Instance.Queue(() =>
        {
          var current = EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.isPlaying;
          if (current != play)
          {
            ourLogger.Verbose("Request to change play mode from model: {0}", play);
            EditorApplication.isPlaying = play;
          }
        });
      });

      model.Pause.Advise(connectionLifetime, pause =>
      {
        MainThreadDispatcher.Instance.Queue(() =>
        {
          ourLogger.Verbose("Request to change pause mode from model: {0}", pause);
          EditorApplication.isPaused = pause;
        });
      });

      model.Step.Advise(connectionLifetime, x =>
      {
        MainThreadDispatcher.Instance.Queue(EditorApplication.Step);
      });

      var onPlaymodeStateChanged = new EditorApplication.CallbackFunction(() => syncPlayState());

// left for compatibility with Unity <= 5.5
#pragma warning disable 618
      connectionLifetime.AddBracket(() => { EditorApplication.playmodeStateChanged += onPlaymodeStateChanged; },
        () => { EditorApplication.playmodeStateChanged -= onPlaymodeStateChanged; });
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

    internal static readonly string LogPath = Path.Combine(Path.Combine(Path.GetTempPath(), "Unity3dRider"), $"EditorPlugin.{Process.GetCurrentProcess().Id}.log");
    internal static OnOpenAssetHandler OpenAssetHandler;

    // Creates and deletes Library/EditorInstance.json containing info about unity instance. Unity 2017.1+ writes this
    // file itself. We'll always overwrite, just to be sure it's up to date. The file contents are exactly the same
    private static void InitializeEditorInstanceJson()
    {
      if (UnityUtils.UnityVersion >= new Version(2017, 1))
        return;

      ourLogger.Verbose("Writing Library/EditorInstance.json");

      var editorInstanceJsonPath = Path.GetFullPath("Library/EditorInstance.json");

      File.WriteAllText(editorInstanceJsonPath, $@"{{
  ""process_id"": {Process.GetCurrentProcess().Id},
  ""version"": ""{UnityUtils.UnityApplicationVersion}""
}}");

      AppDomain.CurrentDomain.DomainUnload += (sender, args) =>
      {
        ourLogger.Verbose("Deleting Library/EditorInstance.json");
        File.Delete(editorInstanceJsonPath);
      };
    }

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

    /// <summary>
    /// Called when Unity is about to open an asset. This method is for pre-2019.2
    /// </summary>
    [OnOpenAsset]
    static bool OnOpenedAsset(int instanceID, int line)
    {
      if (!PluginEntryPoint.IsRiderDefaultEditor())
        return false;

      // if (UnityUtils.UnityVersion >= new Version(2019, 2)
      //   return false;
      return OpenAssetHandler.OnOpenedAsset(instanceID, line, 0);
    }

    /// <summary>
    /// Called when Unity is about to open an asset. This method is new for 2019.2
    /// </summary>
    //[OnOpenAsset] // todo: restore, when we move this code to package, otherwise when OnOpenedAsset is called, there is a LogError in older Unity
    static bool OnOpenedAsset(int instanceID, int line, int column)
    {
      if (!PluginEntryPoint.IsRiderDefaultEditor())
        return false;

      if (UnityUtils.UnityVersion < new Version(2019, 2))
        return false;
      return OpenAssetHandler.OnOpenedAsset(instanceID, line, column);
    }

    public static bool IsLoadedFromAssets()
    {
      var currentDir = Directory.GetCurrentDirectory();
      var location = Assembly.GetExecutingAssembly().Location;
      return location.StartsWith(currentDir, StringComparison.InvariantCultureIgnoreCase);
    }
  }

  public struct UnityModelAndLifetime
  {
    public EditorPluginModel Model;
    public Lifetime Lifetime;

    public UnityModelAndLifetime(EditorPluginModel model, Lifetime lifetime)
    {
      Model = model;
      Lifetime = lifetime;
    }
  }
}