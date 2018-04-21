using System;
using System.Diagnostics;
using System.IO;
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
using JetBrains.Util;
using JetBrains.Util.Logging;
using UnityEditor;
using Application = UnityEngine.Application;
using Debug = UnityEngine.Debug;
using JetBrains.Rider.Unity.Editor.NonUnity;
using UnityEditor.Callbacks;

namespace JetBrains.Rider.Unity.Editor
{
  [InitializeOnLoad]
  public static class PluginEntryPoint
  {
    private static readonly IPluginSettings ourPluginSettings;
    private static readonly RiderPathLocator ourRiderPathLocator;
    public static readonly RProperty<EditorPluginModel> UnityModel = new RProperty<EditorPluginModel>();
    private static readonly UnityEventCollector ourLogEventCollector;

    // This an entry point
    static PluginEntryPoint()
    {
      ourLogEventCollector = new UnityEventCollector();

      ourPluginSettings = new PluginSettings();
      ourRiderPathLocator = new RiderPathLocator(ourPluginSettings);
      var riderPath = ourRiderPathLocator.GetDefaultRiderApp(EditorPrefsWrapper.ExternalScriptEditor,
        RiderPathLocator.GetAllFoundPaths(ourPluginSettings.OperatingSystemFamilyRider));
      if (string.IsNullOrEmpty(riderPath))
        return;

      AddRiderToRecentlyUsedScriptApp(riderPath);
      if (!PluginSettings.RiderInitializedOnce)
      {
        EditorPrefsWrapper.ExternalScriptEditor = riderPath;
        PluginSettings.RiderInitializedOnce = true;
      }

      if (Enabled)
      {
        Init();
      }
    }

    public delegate void MyEventHandler(UnityModelAndLifetime e);
    [UsedImplicitly]
    public static event MyEventHandler OnModelInitialization = delegate {};
    //public static readonly List<Action<UnityModel,Lifetime>> ActionsOnModelInitialization = new List<Action<UnityModel,Lifetime>>();

    internal static bool CheckConnectedToBackendSync()
    {
        var connected = false;
        try
        {
          // HostConnected also means that in Rider and in Unity the same solution is opened
          connected = UnityModel.Maybe.ValueOrDefault.IsBackendConnected.Sync(RdVoid.Instance,
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
      return ourOpenAssetHandler.CallRider(args);
    }
    
    private static bool ourInitialized;
    
    private static readonly ILog ourLogger = Log.GetLog("RiderPlugin");
    
    internal static string SlnFile;

    public static bool Enabled
    {
      get
      {
        var defaultApp = EditorPrefsWrapper.ExternalScriptEditor;
        return !string.IsNullOrEmpty(defaultApp) && Path.GetFileName(defaultApp).ToLower().Contains("rider");
      }
    }

    private static void Init()
    {
      var projectDirectory = Directory.GetParent(Application.dataPath).FullName;

      var projectName = Path.GetFileName(projectDirectory);
      SlnFile = Path.GetFullPath($"{projectName}.sln");

      InitializeEditorInstanceJson();

      // https://github.com/JetBrains/resharper-unity/issues/527#issuecomment-383326065
      // process csproj files once per Unity process 
      if (EditorPrefs.GetInt("Rider_UnityProcessId", 0) != Process.GetCurrentProcess().Id)
      {
        CsprojAssetPostprocessor.OnGeneratedCSProjectFiles();
        EditorPrefs.SetInt("Rider_UnityProcessId", Process.GetCurrentProcess().Id);
      }

      Log.DefaultFactory = new RiderLoggerFactory();

      var lifetimeDefinition = Lifetimes.Define(EternalLifetime.Instance);
      var lifetime = lifetimeDefinition.Lifetime;

      AppDomain.CurrentDomain.DomainUnload += (EventHandler) ((_, __) =>
      {
        ourLogger.Verbose("lifetimeDefinition.Terminate");
        lifetimeDefinition.Terminate();
      });

      if (PluginSettings.SelectedLoggingLevel >= LoggingLevel.VERBOSE)
        Debug.Log($"Rider plugin initialized. LoggingLevel: {PluginSettings.SelectedLoggingLevel}. Change it in Unity Preferences -> Rider. Logs path: {LogPath}.");

      try
      {
        var riderProtocolController = new RiderProtocolController(MainThreadDispatcher.Instance, lifetime);

        var serializers = new Serializers();
        var identities = new Identities(IdKind.Server);
        
        MainThreadDispatcher.AssertThread();

        riderProtocolController.Wire.Connected.WhenTrue(lifetime, connectionLifetime =>
        {
          var protocol = new Protocol("UnityEditorPlugin", serializers, identities, MainThreadDispatcher.Instance, riderProtocolController.Wire);
          ourLogger.Log(LoggingLevel.VERBOSE, "Create UnityModel and advise for new sessions...");
          var model = new EditorPluginModel(connectionLifetime, protocol);
          AdviseUnityActions(model, connectionLifetime);
          AdviseModel(model);
          OnModelInitialization(new UnityModelAndLifetime(model, connectionLifetime));
          AdviseRefresh(model);
          
          model.FullPluginPath.Advise(connectionLifetime, AdditionalPluginsInstaller.UpdateSelf);
          model.ApplicationVersion.SetValue(UnityUtils.UnityVersion.ToString());
          model.ScriptingRuntime.SetValue(UnityUtils.ScriptingRuntime);
          
          ourLogger.Verbose("UnityModel initialized.");
          UnityModel.SetValue(model);
          new UnityEventLogSender(ourLogEventCollector, connectionLifetime);
        });
      }
      catch (Exception ex)
      {
        ourLogger.Error("Init Rider Plugin " + ex);
      }

      ourOpenAssetHandler = new OnOpenAssetHandler(UnityModel, ourRiderPathLocator, ourPluginSettings, SlnFile);
      ourInitialized = true;
    }

    private static void AdviseModel(EditorPluginModel modelValue)
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
          UnityModel?.Maybe.ValueOrDefault?.Pause.SetValue(isPaused);
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

    internal static readonly string  LogPath = Path.Combine(Path.Combine(Path.GetTempPath(), "Unity3dRider"), DateTime.Now.ToString("yyyy-MM-ddT-HH-mm-ss") + ".log");
    private static OnOpenAssetHandler ourOpenAssetHandler;

    /// <summary>
    /// Creates and deletes Library/EditorInstance.json containing info about unity instance
    /// </summary>
    private static void InitializeEditorInstanceJson()
    {
      ourLogger.Verbose("Writing Library/EditorInstance.json");

      var editorInstanceJsonPath = Path.GetFullPath("Library/EditorInstance.json");

      File.WriteAllText(editorInstanceJsonPath, $@"{{
  ""process_id"": {Process.GetCurrentProcess().Id},
  ""version"": ""{Application.unityVersion}"",
  ""app_path"": ""{EditorApplication.applicationPath}"",
  ""app_contents_path"": ""{EditorApplication.applicationContentsPath}"",
  ""attach_allowed"": ""{EditorPrefs.GetBool("AllowAttachedDebuggingOfEditor", true)}"",
  ""is_loaded_from_assets"": ""{IsLoadedFromAssets()}"",
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
    /// Called when Unity is about to open an asset.
    /// </summary>
    [OnOpenAsset]
    private static bool OnOpenedAsset(int instanceID, int line)
    {
      if (!Enabled) 
        return false;
      if (!ourInitialized)
      {
        // make sure the plugin was initialized first.
        // this can happen in case "Rider" was set as the default scripting app only after this plugin was imported.
        Init();
      }
      
      return ourOpenAssetHandler.OnOpenedAsset(instanceID, line);
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

// Developed with JetBrains Rider =)