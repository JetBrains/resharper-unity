using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using JetBrains.Collections.Viewable;
using JetBrains.Core;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Rd;
using JetBrains.Rd.Base;
using JetBrains.Rd.Impl;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.Model.Unity;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Rider.Unity.Editor.NonUnity;
using JetBrains.Rider.PathLocator;
using JetBrains.Rider.Unity.Editor.FindUsages;
using JetBrains.Rider.Unity.Editor.Profiler;
using JetBrains.Rider.Unity.Editor.UnitTesting;
using JetBrains.Rider.Unity.Editor.Utils;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace JetBrains.Rider.Unity.Editor
{
  internal static class UnityEditorProtocol
  {
    private static ILog ourLogger;
    private static long ourInitTime;

    // We cannot guarantee a valid, connected model.
    // Model creation, lifetime termination, protocol callbacks and Unity API events are all called on the main thread,
    // so we know that inside a callback, the model has a lifetime that has not yet been terminated.
    // But a model can lose its socket connection at any time, even mid-callback. If the connection is closed
    // gracefully, the Model.Connected property is updated on a background thread. But there is an inherent race
    // condition here. Even if we check Model.Connected, the connection might disappear before we invoke the protocol
    // method.
    // More importantly, we don't get notified about socket connection failing until the model/connection lifetime is
    // queued for termination on the main thread.
    // In short: we can assume that a model is valid inside a callback. We cannot assume that it is connected. We can
    // use Model.Connected to potentially reduce the scope of the race condition, but we will not be notified if the
    // connection fails.
#region Details
    // Details:
    // * The protocol's Wire creates a background thread for socket communication. It sets/resets the Wire.Connected
    //   property before/after listening for incoming messages. It uses the given scheduler (MainThreadDispatcher) to
    //   queue changing the property to the correct thread - the main thread
    // * The model is created when Wire.Connected becomes true on the scheduled thread
    // * Protocol messages are received on the background thread and notified via the scheduler, so callbacks happen on
    //   the main thread
    // * Unity API events are always called on the main thread
    // * When the Wire stops receiving incoming events (either gracefully or with a socket exception), Wire.Connected is
    //   set to false. This is queued with the scheduler and the property is set on the main thread. The existing
    //   lifetime is terminated and handlers are immediately invoked, still on the main thread
    // * The model's base class (RdExtBase) sends a handshake when it's created. When the other side responds, the
    //   Model.Connected property is set to true. This is invoked on the model's default scheduler, which is the
    //   SynchronousScheduler, so Model.Connected is set (and notified) on a background thread
    // * If the other side (Rider) is shut down gracefully, it sends a disconnected event. Again, this is invoked on the
    //   model's default scheduler, which means Model.Connected is set and notified on a background thread
    // * If the other side does not shut down gracefully, it does not send a disconnected event, and Model.Connected
    //   remains true. Any access to the socket will throw an exception, causing the protocol's background thread to
    //   close down, queuing Wire.Connected = false with the scheduler
#endregion
    public static readonly IViewableList<BackendUnityModel> Models = new ViewableList<BackendUnityModel>();

    public static void Initialise(Lifetime appDomainLifetime, long initTime, ILog logger)
    {
      ourLogger = logger;
      ourInitTime = initTime;

      var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
      var solutionNames = new List<string> { currentDirectory.Name };

      ourLogger.Verbose("Initialising protocol. Looking for solution files");

      var solutionFiles = currentDirectory.GetFiles("*.sln", SearchOption.TopDirectoryOnly);
      foreach (var solutionFile in solutionFiles)
      {
        var solutionName = Path.GetFileNameWithoutExtension(solutionFile.FullName);
        if (!solutionName.Equals(currentDirectory.Name))
        {
          solutionNames.Add(solutionName);
        }
      }

      var protocols = new List<ProtocolInstance>();

      // If any protocol connection is lost, we will drop all connections and recreate them
      var allProtocolsLifetimeDefinition = appDomainLifetime.CreateNested();
      foreach (var solutionName in GetSolutionNames())
      {
        var port = CreateProtocolForSolution(appDomainLifetime, allProtocolsLifetimeDefinition.Lifetime, solutionName,
          () => allProtocolsLifetimeDefinition.Terminate());

        if (port == -1)
          continue;

        protocols.Add(new ProtocolInstance(solutionName, port));
      }

      if (!protocols.Any())
      {
        ourLogger.Warn("Initialising protocol failed.");
        return;
      }

      allProtocolsLifetimeDefinition.Lifetime.OnTermination(() =>
      {
        if (appDomainLifetime.IsAlive)
        {
          ourLogger.Verbose("Schedule recreating protocol, project lifetime is alive");
          new Thread(() =>
          {
            Thread.Sleep(1000);

            if (appDomainLifetime.IsAlive)
            {
              ourLogger.Verbose("Before MainThreadDispatcher.Instance.Queue(() =>");
              MainThreadDispatcher.Instance.Queue(() =>
              {
                ourLogger.Verbose("Inside MainThreadDispatcher.Instance.Queue(() =>");
                if (appDomainLifetime.IsAlive)
                {
                  ourLogger.Verbose("Recreating protocol, project lifetime is alive");
                  Initialise(appDomainLifetime, initTime, logger);
                }
              });
            }
          }).Start();
        }
        else
        {
          ourLogger.Verbose("Protocol will be recreated on next domain load, plugin lifetime is not alive");
        }
      });

      ourLogger.Verbose("Writing Library/ProtocolInstance.json");
      var protocolInstancePath = Path.GetFullPath("Library/ProtocolInstance.json");
      var result = ProtocolInstance.ToJson(protocols);
      File.WriteAllText(protocolInstancePath, result);

      // TODO: Will this cause problems if we call Initialise a second time?
      // Perhaps we need another lifetime?
      appDomainLifetime.OnTermination(() =>
      {
        ourLogger.Verbose("Deleting Library/ProtocolInstance.json");
        File.Delete(protocolInstancePath);
      });
    }

    private static HashSet<string> GetSolutionNames()
    {
      // Get a list of all the solutions in the Unity project. We'll have at least the generated solution, but there
      // might be others, e.g. class libraries. We'll create a protocol connection for all such solutions
      var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
      var solutionNames = new HashSet<string> { currentDirectory.Name };

      var solutionFiles = currentDirectory.GetFiles("*.sln", SearchOption.TopDirectoryOnly);
      foreach (var solutionFile in solutionFiles)
      {
        var solutionName = Path.GetFileNameWithoutExtension(solutionFile.FullName);
        solutionNames.Add(solutionName);
      }

      return solutionNames;
    }

    private static int CreateProtocolForSolution(Lifetime appDomainLifetime, Lifetime lifetime, string solutionName, Action onDisconnected)
    {
      ourLogger.Verbose($"Initialising protocol for {solutionName}");
      try
      {
        var dispatcher = MainThreadDispatcher.Instance;
        var currentWireAndProtocolLifetimeDef = lifetime.CreateNested();
        var currentWireAndProtocolLifetime = currentWireAndProtocolLifetimeDef.Lifetime;

        var riderProtocolController = new RiderProtocolController(dispatcher, currentWireAndProtocolLifetime);

        var serializers = new Serializers();
        var identities = new Identities(IdKind.Server);

        MainThreadDispatcher.AssertThread();
        var protocol = new Protocol("UnityEditorPlugin" + solutionName, serializers, identities,
          MainThreadDispatcher.Instance, riderProtocolController.Wire, currentWireAndProtocolLifetime);
        riderProtocolController.Wire.Connected.WhenTrue(currentWireAndProtocolLifetime, connectionLifetime =>
        {
          ourLogger.Log(LoggingLevel.VERBOSE, "Create UnityModel and advise for new sessions...");

          var model = new BackendUnityModel(connectionLifetime, protocol);

          SetApplicationData(model);
          SetProjectSettings(model);
          AdvisePlayControls(model, connectionLifetime);
          AdviseOnGetEditorState(model);
          AdviseOnRefresh(model);
          AdviseShowPreferences(model, connectionLifetime, ourLogger);
          AdviseOnGenerateUIElementsSchema(model);
          AdviseOnExitUnity(model);
          AdviseOnRunMethod(model);
          AdviseOnStartProfiling(model);
          AdviseLoggingStateChangeTimes(connectionLifetime, model);

          BuildPipelineModelHelper.Advise(connectionLifetime, model);

#if UNITY_5_6_OR_NEWER
          UnitTestingModelHelper.Advise(appDomainLifetime, connectionLifetime, model);
          FindUsagesModelHelper.Advise(connectionLifetime, model);
          UnsavedChangesModelHelper.Advise(connectionLifetime, model);
#endif

#if UNITY_2019_2_OR_NEWER
          PackageManagerModelHelper.Advise(connectionLifetime, model);
          ProfilerWindowEventsHandler.Advise(connectionLifetime, new UnityProfilerModel(connectionLifetime, protocol));
#endif

          Models.AddLifetimed(connectionLifetime, model);

          ourLogger.Verbose("UnityModel initialized.");

          connectionLifetime.OnTermination(() =>
          {
            ourLogger.Verbose($"Connection lifetime is not alive for {solutionName}, destroying protocol");
            onDisconnected();
          });
        });

        return riderProtocolController.Wire.Port;
      }
      catch (Exception ex)
      {
        ourLogger.Error("Init Rider Plugin " + ex);
        return -1;
      }
    }

    private static void SetApplicationData(BackendUnityModel model)
    {
      var paths = GetLogPaths();
      model.UnityApplicationData.Value = new UnityApplicationData(
        EditorApplication.applicationPath,
        EditorApplication.applicationContentsPath,
        UnityUtils.UnityApplicationVersion,
        paths[0], paths[1],
        Process.GetCurrentProcess().Id);

      model.UnityApplicationSettings.ScriptCompilationDuringPlay.Value = UnityUtils.SafeScriptCompilationDuringPlay;
    }

    private static string[] GetLogPaths()
    {
      // https://docs.unity3d.com/Manual/LogFiles.html
      //PlayerSettings.productName;
      //PlayerSettings.companyName;
      //~/Library/Logs/Unity/Editor.log
      //C:\Users\username\AppData\Local\Unity\Editor\Editor.log
      //~/.config/unity3d/Editor.log

      var editorLogPath = string.Empty;
      var playerLogPath = string.Empty;

      switch (PluginSettings.SystemInfoRiderPlugin.OS)
      {
        case OS.Windows:
        {
          var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
          editorLogPath = Path.Combine(localAppData, @"Unity\Editor\Editor.log");
          var userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
          if (!string.IsNullOrEmpty(userProfile))
          {
            var folder = Path.Combine(userProfile, @"AppData\LocalLow", PlayerSettings.companyName, PlayerSettings.productName);
            playerLogPath = Path.Combine(folder, File.Exists(Path.Combine(folder, "output_log.txt")) ? "output_log.txt" : "Player.log");
          }

          break;
        }
        case OS.MacOSX:
        {
          var home = Environment.GetEnvironmentVariable("HOME");
          if (!string.IsNullOrEmpty(home))
          {
            editorLogPath = Path.Combine(home, "Library/Logs/Unity/Editor.log");
            playerLogPath = Path.Combine(home, "Library/Logs", PlayerSettings.companyName, PlayerSettings.productName, "Player.log");
          }

          break;
        }
        case OS.Linux:
        {
          var home = Environment.GetEnvironmentVariable("HOME");
          if (!string.IsNullOrEmpty(home))
          {
            editorLogPath = Path.Combine(home, ".config/unity3d/Editor.log");
            playerLogPath = Path.Combine(home, ".config/unity3d", PlayerSettings.companyName, PlayerSettings.productName, "Player.log");
          }

          break;
        }
      }

      return new[] { editorLogPath, playerLogPath };
    }

    private static void SetProjectSettings(BackendUnityModel model)
    {
      model.UnityProjectSettings.ScriptingRuntime.Value = UnityUtils.ScriptingRuntime;
      var path = EditorUserBuildSettings.GetBuildLocation(EditorUserBuildSettings.selectedStandaloneTarget);
      if (PluginSettings.SystemInfoRiderPlugin.OS == OS.MacOSX)
        path = Path.Combine(Path.Combine(Path.Combine(path, "Contents"), "MacOS"), PlayerSettings.productName);
      if (!string.IsNullOrEmpty(path) && File.Exists(path))
        model.UnityProjectSettings.BuildLocation.Value = path;
    }

    private static void AdvisePlayControls(BackendUnityModel model, Lifetime connectionLifetime)
    {
      var syncPlayState = new Action(() =>
      {
        MainThreadDispatcher.AssertThread();

        var isPlaying = EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isPlaying;

        if (!model.PlayControls.Play.HasValue() ||
            model.PlayControls.Play.HasValue() && model.PlayControls.Play.Value != isPlaying)
        {
          ourLogger.Verbose("Reporting play mode change to model: {0}", isPlaying);
          model.PlayControls.Play.SetValue(isPlaying);
        }

        var isPaused = EditorApplication.isPaused;
        if (!model.PlayControls.Pause.HasValue() ||
            model.PlayControls.Pause.HasValue() && model.PlayControls.Pause.Value != isPaused)
        {
          ourLogger.Verbose("Reporting pause mode change to model: {0}", isPaused);
          model.PlayControls.Pause.SetValue(isPaused);
        }
      });

      syncPlayState();

      model.PlayControls.Play.Advise(connectionLifetime, play =>
      {
        MainThreadDispatcher.AssertThread();

        var current = EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.isPlaying;
        if (current != play)
        {
          ourLogger.Verbose("Request to change play mode from model: {0}", play);
          EditorApplication.isPlaying = play;
        }
      });

      model.PlayControls.Pause.Advise(connectionLifetime, pause =>
      {
        MainThreadDispatcher.AssertThread();

        ourLogger.Verbose("Request to change pause mode from model: {0}", pause);
        EditorApplication.isPaused = pause;
      });

      model.PlayControls.Step.Advise(connectionLifetime, _ => EditorApplication.Step());
      PlayModeStateTracker.Current.Advise(connectionLifetime, _ => syncPlayState());
    }

    private static void AdviseOnGetEditorState(BackendUnityModel modelValue)
    {
      modelValue.GetUnityEditorState.Set(_ =>
      {
        if (EditorApplication.isPaused)
          return UnityEditorState.Pause;

        if (EditorApplication.isPlaying)
          return UnityEditorState.Play;

        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
          return UnityEditorState.Refresh;

        return UnityEditorState.Idle;
      });
    }

    private static void AdviseOnRefresh(BackendUnityModel model)
    {
      model.Refresh.Set((_, force) =>
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

        MainThreadDispatcher.AssertThread();

        ourLogger.Verbose("Refresh: SyncSolution Enqueue");
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
            RiderPackageInterop.SyncSolution();
          }
          catch (Exception e)
          {
            ourLogger.Error(e, "Refresh failed with exception");
          }
          finally
          {
            EditorApplication.update += SendResult;
          }
        }
        else
        {
          if (EditorApplication.isPlaying)
          {
            refreshTask.Set(Unit.Instance);
            ourLogger.Verbose("Avoid calling Refresh, when EditorApplication.isPlaying.");
          }
          else if (!EditorPrefsWrapper.AutoRefresh)
          {
            refreshTask.Set(Unit.Instance);
            ourLogger.Verbose("AutoRefresh is disabled by Unity preferences.");
          }
          else
          {
            refreshTask.Set(Unit.Instance);
            ourLogger.Verbose("Avoid calling Refresh, for the unknown reason.");
          }
        }

        return refreshTask;
      });
    }

    private static void AdviseShowPreferences(BackendUnityModel model, Lifetime connectionLifetime, ILog log)
    {
      model.ShowPreferences.Advise(connectionLifetime, result =>
      {
        if (result == null) return;

        MainThreadDispatcher.AssertThread();

        try
        {
          var tab = UnityUtils.UnityVersion >= new Version(2018, 2) ? "_General" : "Rider";

          var type = typeof(SceneView).Assembly.GetType("UnityEditor.SettingsService");
          if (type != null)
          {
            // 2018+
            var method = type.GetMethod("OpenUserPreferences", BindingFlags.Static | BindingFlags.Public);

            if (method == null)
              log.Error("'OpenUserPreferences' was not found");
            else
              method.Invoke(null, new object[] { $"Preferences/{tab}" });
          }
          else
          {
            // 5.5, 2017 ...
            type = typeof(SceneView).Assembly.GetType("UnityEditor.PreferencesWindow");
            var method = type?.GetMethod("ShowPreferencesWindow", BindingFlags.Static | BindingFlags.NonPublic);

            if (method == null)
              log.Error("'ShowPreferencesWindow' was not found");
            else
              method.Invoke(null, null);
          }
        }
        catch (Exception ex)
        {
          log.Error("Show preferences " + ex);
        }
      });
    }

    private static void AdviseOnGenerateUIElementsSchema(BackendUnityModel model)
    {
      model.GenerateUIElementsSchema.Set(_ => UIElementsSupport.GenerateSchema());
    }

    private static void AdviseOnExitUnity(BackendUnityModel model)
    {
      model.ExitUnity.Set((_, __) =>
      {
        var task = new RdTask<bool>();

        MainThreadDispatcher.AssertThread();

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

        return task;
      });
    }

    private static void AdviseOnRunMethod(BackendUnityModel model)
    {
      model.RunMethodInUnity.Set((lifetime, data) =>
      {
        var task = new RdTask<RunMethodResult>();

        MainThreadDispatcher.AssertThread();

        if (!lifetime.IsAlive)
        {
          task.SetCancelled();
          return task;
        }

        try
        {
          ourLogger.Verbose($"Attempt to execute {data.MethodName}");
          var assemblies = AppDomain.CurrentDomain.GetAssemblies();
          var assembly = assemblies
            .FirstOrDefault(a => a.GetName().Name.Equals(data.AssemblyName));
          if (assembly == null)
            throw new Exception($"Could not find {data.AssemblyName} assembly in current AppDomain");

          var type = assembly.GetType(data.TypeName);
          if (type == null)
            throw new Exception($"Could not find {data.TypeName} in assembly {data.AssemblyName}.");

          var method = type.GetMethod(data.MethodName,
            BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

          if (method == null)
            throw new Exception($"Could not find {data.MethodName} in type {data.TypeName}");

          try
          {
            method.Invoke(null, null);
          }
          catch (Exception e)
          {
            Debug.LogException(e);
          }

          task.Set(new RunMethodResult(true, string.Empty, string.Empty));
        }
        catch (Exception e)
        {
          ourLogger.Log(LoggingLevel.WARN, $"Execute {data.MethodName} failed.", e);
          task.Set(new RunMethodResult(false, e.Message, e.StackTrace));
        }

        return task;
      });
    }

    private static void AdviseOnStartProfiling(BackendUnityModel model)
    {
      model.StartProfiling.Set((_, data) =>
      {
        MainThreadDispatcher.AssertThread();

        try
        {
          UnityProfilerApiInterop.StartProfiling(data.UnityProfilerApiPath, data.NeedRestartScripts);

          var current = EditorApplication.isPlayingOrWillChangePlaymode && EditorApplication.isPlaying;
          if (current != data.EnterPlayMode)
          {
            ourLogger.Verbose("StartProfiling. Request to change play mode from model: {0}", data.EnterPlayMode);
            EditorApplication.isPlaying = data.EnterPlayMode;
          }
        }
        catch (Exception e)
        {
          if (PluginSettings.SelectedLoggingLevel >= LoggingLevel.VERBOSE)
            Debug.LogError(e);
          throw;
        }

        return Unit.Instance;
      });

      model.StopProfiling.Set((_, data) =>
      {
        MainThreadDispatcher.AssertThread();

        try
        {
          UnityProfilerApiInterop.StopProfiling(data.UnityProfilerApiPath);
        }
        catch (Exception e)
        {
          if (PluginSettings.SelectedLoggingLevel >= LoggingLevel.VERBOSE)
            Debug.LogError(e);
          throw;
        }

        return Unit.Instance;
      });
    }

    private static void AdviseLoggingStateChangeTimes(Lifetime modelLifetime, BackendUnityModel model)
    {
      model.ConsoleLogging.LastInitTime.Value = ourInitTime;

      PlayModeStateTracker.Current.Advise(modelLifetime, state =>
      {
        if (state == PlayModeState.Playing)
          model.ConsoleLogging.LastPlayTime.Value = DateTime.UtcNow.Ticks;
      });
    }
  }
}

// Empty namespaces to avoid #if for Unity 4.7
// ReSharper disable EmptyNamespace
namespace JetBrains.Rider.Unity.Editor.FindUsages {}
namespace JetBrains.Rider.Unity.Editor.UnitTesting {}
