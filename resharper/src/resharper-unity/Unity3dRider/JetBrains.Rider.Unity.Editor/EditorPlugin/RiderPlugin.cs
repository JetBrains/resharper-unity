using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework;
using JetBrains.Platform.RdFramework.Base;
using JetBrains.Platform.RdFramework.Impl;
using JetBrains.Platform.RdFramework.Tasks;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.Platform.Unity.Model;
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
  public static class RiderPlugin
  {
    private static readonly IPluginSettings ourPluginSettings;
    private static readonly RiderPathLocator ourRiderPathLocator;

    static RiderPlugin()
    {
      var logSender = new UnityEventLogSender();;
      logSender.UnityLogRegisterCallBack();
      
      ourPluginSettings = new PluginSettings();
      ourRiderPathLocator = new RiderPathLocator(ourPluginSettings);
      var riderPath = ourRiderPathLocator.GetDefaultRiderApp(EditorPrefsWrapper.ExternalScriptEditor,
        RiderPathLocator.GetAllFoundPaths(ourPluginSettings.OperatingSystemFamilyRider));
      if (String.IsNullOrEmpty(riderPath))
        return;

      AddRiderToRecentlyUsedScriptApp(riderPath);
      if (!PluginSettings.RiderInitializedOnce)
      {
        EditorPrefsWrapper.ExternalScriptEditor = riderPath;
        PluginSettings.RiderInitializedOnce = true;
      }

      if (Enabled)
      {
        InitRiderPlugin();
      }
    }

    private static bool ourInitialized;
    internal static string SlnFile;
    private static readonly ILog ourLogger = Log.GetLog("RiderPlugin");
    public static readonly RProperty<UnityModel> Model = new RProperty<UnityModel>();

    public static bool Enabled
    {
      get
      {
        var defaultApp = EditorPrefsWrapper.ExternalScriptEditor;
        return !String.IsNullOrEmpty(defaultApp) && Path.GetFileName(defaultApp).ToLower().Contains("rider");
      }
    }

    private static void InitRiderPlugin()
    {
      var projectDirectory = Directory.GetParent(Application.dataPath).FullName;

      var projectName = Path.GetFileName(projectDirectory);
      SlnFile = Path.GetFullPath($"{projectName}.sln");

      InitializeEditorInstanceJson();

      // for the case when files were changed and user just alt+tab to unity to make update, we want to fire
      RiderAssetPostprocessor.OnGeneratedCSProjectFiles();

      Log.DefaultFactory = new RiderLoggerFactory();

      var lifetimeDefinition = Lifetimes.Define(EternalLifetime.Instance);
      var lifetime = lifetimeDefinition.Lifetime;

      AppDomain.CurrentDomain.DomainUnload += (EventHandler) ((_, __) =>
      {
        ourLogger.Verbose("lifetimeDefinition.Terminate");
        lifetimeDefinition.Terminate();
      });

      Debug.Log($"Rider plugin initialized. Further logs in: {LogPath}");

      try
      {
        var riderProtocolController = new RiderProtocolController(MainThreadDispatcher.Instance, lifetime);

        var serializers = new Serializers();
        var identities = new Identities(IdKind.Server);
        
        MainThreadDispatcher.AssertThread();
        
        riderProtocolController.Wire.Connected.View(lifetime, (lt, connected) =>
        {
          if (connected)
          {
            var protocol = new Protocol(serializers, identities, MainThreadDispatcher.Instance, riderProtocolController.Wire);
            ourLogger.Log(LoggingLevel.VERBOSE, "Create UnityModel and advise for new sessions...");

            Model.Value = CreateModel(protocol, lt);
          }
          else
            Model.Value = null;
        });
      }
      catch (Exception ex)
      {
        ourLogger.Error("Init Rider Plugin " + ex);
      }

      ourInitialized = true;
    }

    private static UnityModel CreateModel(Protocol protocol, Lifetime lt)
    {
      var isPlayingAction = new Action(() =>
      {
        MainThreadDispatcher.Instance.Queue(() =>
        {
          var isPlaying = EditorApplication.isPlaying;
          Model?.Maybe.ValueOrDefault?.Play.SetValue(isPlaying);

          var isPaused = EditorApplication.isPaused;
          Model?.Maybe.ValueOrDefault?.Pause.SetValue(isPaused);
        });
      });
      var model = new UnityModel(lt, protocol);
      isPlayingAction(); // get Unity state
      model.Play.Advise(lt, play =>
      {
        MainThreadDispatcher.Instance.Queue(() =>
        {
          var res = EditorApplication.isPlaying;
          if (res != play)
            EditorApplication.isPlaying = play;
        });
      });

      model.Pause.Advise(lt, pause =>
      {
        MainThreadDispatcher.Instance.Queue(() =>
        {
          EditorApplication.isPaused = pause;
        });
      });
      model.LogModelInitialized.SetValue(new UnityLogModelInitialized());

      model.Refresh.Set((l, x) =>
      {
        var task = new RdTask<RdVoid>();
        MainThreadDispatcher.Instance.Queue(() =>
        {
          UnityUtils.SyncSolution();
          task.Set(RdVoid.Instance);
        });
        return task;
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
      lt.AddBracket(() => { EditorApplication.playmodeStateChanged += isPlayingHandler; },
        () => { EditorApplication.playmodeStateChanged -= isPlayingHandler; });
#pragma warning restore 618
      isPlayingHandler();
      
      // new api - not present in Unity 5.5
      //lt.AddBracket(() => { EditorApplication.pauseStateChanged+= IsPauseStateChanged(model);},
      //  () => { EditorApplication.pauseStateChanged -= IsPauseStateChanged(model); });
      

      return model;
    }

    // new api - not present in Unity 5.5
    // private static Action<PauseState> IsPauseStateChanged(UnityModel model)
    //    {
    //      return state => model?.Pause.SetValue(state == PauseState.Paused);
    //    }

    internal static readonly string  LogPath = Path.Combine(Path.Combine(Path.GetTempPath(), "Unity3dRider"), DateTime.Now.ToString("yyyy-MM-ddT-HH-mm-ss") + ".log");

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
  ""attach_allowed"": ""{EditorPrefs.GetBool("AllowAttachedDebuggingOfEditor", true)}""
}}");

      AppDomain.CurrentDomain.DomainUnload += (sender, args) =>
      {
        ourLogger.Verbose("Deleting Library/EditorInstance.json");
        File.Delete(editorInstanceJsonPath);
      };
    }

    /// <summary>
    /// Asset Open Callback (from Unity)
    /// </summary>
    /// <remarks>
    /// Called when Unity is about to open an asset.
    /// </remarks>
    [OnOpenAsset()]
    private static bool OnOpenedAsset(int instanceID, int line)
    {
      if (!Enabled) 
        return false;
      if (!ourInitialized)
      {
        // make sure the plugin was initialized first.
        // this can happen in case "Rider" was set as the default scripting app only after this plugin was imported.
        InitRiderPlugin();
      }

      // determine asset that has been double clicked in the project view
      var selected = EditorUtility.InstanceIDToObject(instanceID);

      var assetFilePath = Path.GetFullPath(AssetDatabase.GetAssetPath(selected));
      if (!(selected.GetType().ToString() == "UnityEditor.MonoScript" ||
            selected.GetType().ToString() == "UnityEngine.Shader" ||
            (selected.GetType().ToString() == "UnityEngine.TextAsset" &&
//#i f UNITY_5 || UNITY_5_5_OR_NEWER
//             EditorSettings.projectGenerationUserExtensions.Contains(Path.GetExtension(assetFilePath).Substring(1))
//#e lse
            EditorSettings.externalVersionControl.Contains(Path.GetExtension(assetFilePath).Substring(1))
//#e ndif
            )))
        return false;

      UnityUtils.SyncSolution(); // added to handle opening file, which was just recently created.

      var model = Model.Maybe.ValueOrDefault;
      if (model!=null)
      {
        var connected = false;
        try
        {
          // HostConnected also means that in Rider and in Unity the same solution is opened
          connected = model.IsClientConnected.Sync(RdVoid.Instance,
            new RpcTimeouts(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(200)));
        }
        catch (Exception)
        {
          ourLogger.Verbose("Rider Protocol not connected.");
        }
        if (connected)
        {
          var col = 0;
          ourLogger.Verbose("Calling OpenFileLineCol: {0}, {1}, {2}", assetFilePath, line, col);
          model.OpenFileLineCol.Start(new RdOpenFileArgs(assetFilePath, line, col));
          if (model.RiderProcessId.HasValue())
            ActivateWindow(model.RiderProcessId.Value);
          else
            ActivateWindow();
          //task.Result.Advise(); todo: fallback to CallRider, if returns false
          return true;
        }
      }

      var args = String.Format("{0}{1}{0} --line {2} {0}{3}{0}", "\"", SlnFile, line, assetFilePath);
      return CallRider(args);

    }
    
    internal static bool CallRider(string args)
    {
      var defaultApp = ourRiderPathLocator.GetDefaultRiderApp(EditorPrefsWrapper.ExternalScriptEditor, RiderPathLocator.GetAllFoundPaths(ourPluginSettings.OperatingSystemFamilyRider));
      if (String.IsNullOrEmpty(defaultApp))
      {
        return false;
      }

      var proc = new Process();
      if (ourPluginSettings.OperatingSystemFamilyRider == OperatingSystemFamilyRider.MacOSX)
      {
        proc.StartInfo.FileName = "open";
        proc.StartInfo.Arguments = String.Format("-n {0}{1}{0} --args {2}", "\"", "/" + defaultApp, args);
        ourLogger.Verbose("{0} {1}", proc.StartInfo.FileName, proc.StartInfo.Arguments);
      }
      else
      {
        proc.StartInfo.FileName = defaultApp;
        proc.StartInfo.Arguments = args;
        ourLogger.Verbose("{2}{0}{2}" + " {1}", proc.StartInfo.FileName, proc.StartInfo.Arguments, "\"");
      }

      proc.StartInfo.UseShellExecute = false;
      proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
      proc.StartInfo.CreateNoWindow = true;
      proc.StartInfo.RedirectStandardOutput = true;
      proc.Start();

      ActivateWindow();
      return true;
    }

    private static void ActivateWindow(int? processId=null)
    {
      if (ourPluginSettings.OperatingSystemFamilyRider == OperatingSystemFamilyRider.Windows)
      {
        try
        {
          var process = processId == null ? GetRiderProcess() : Process.GetProcessById((int)processId);
          if (process != null)
          {
            // Collect top level windows
            var topLevelWindows = User32Dll.GetTopLevelWindowHandles();
            // Get process main window title
            var windowHandle = topLevelWindows.FirstOrDefault(hwnd => User32Dll.GetWindowProcessId(hwnd) == process.Id);
            ourLogger.Verbose("ActivateWindow: {0} {1}", process.Id, windowHandle);
            if (windowHandle != IntPtr.Zero)
            {
              //User32Dll.ShowWindow(windowHandle, 9); //SW_RESTORE = 9
              User32Dll.SetForegroundWindow(windowHandle);
            }
          }
        }
        catch (Exception e)
        {
          ourLogger.Warn("Exception on ActivateWindow: " + e);
        }
      }
    }

    private static Process GetRiderProcess()
    {
      var process = Process.GetProcesses().FirstOrDefault(p =>
      {
        string processName;
        try
        {
          processName =
            p.ProcessName; // some processes like kaspersky antivirus throw exception on attempt to get ProcessName
        }
        catch (Exception)
        {
          return false;
        }

        return !p.HasExited && processName.ToLower().Contains("rider");
      });
      return process;
    }

    // The default "Open C# Project" menu item will use the external script editor to load the .sln
    // file, but unless Unity knows the external script editor can properly load solutions, it will
    // also launch MonoDevelop (or the OS registered app for .sln files). This menu item side steps
    // that issue, and opens the solution in Rider without opening MonoDevelop as well.
    // Unity 2017.1 and later recognise Rider as an app that can load solutions, so this menu isn't
    // needed in newer versions.
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
  }
}

// Developed with JetBrains Rider =)