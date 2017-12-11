using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Platform.RdFramework;
using JetBrains.Platform.RdFramework.Tasks;
using JetBrains.Platform.Unity.Model;
using JetBrains.Rider.Unity.Editor;
using JetBrains.Util;
using JetBrains.Util.Logging;
using UnityEditor;
using UnityEngine;
using Application = UnityEngine.Application;
using Debug = UnityEngine.Debug;

namespace Plugins.Editor.JetBrains
{
  [InitializeOnLoad]
  public static class RiderPlugin
  {
    static RiderPlugin()
    {
      var riderPath = GetDefaultApp();
      if (!RiderPathExist(riderPath))
        return;

      AddRiderToRecentlyUsedScriptApp(riderPath, "RecentlyUsedScriptApp");
      if (!RiderInitializedOnce)
      {
        SetExternalScriptEditor(riderPath);
        RiderInitializedOnce = true;
      }
      if (Enabled)
      {
        InitRiderPlugin();
      }
    }
    
    public static readonly string logPath = Path.Combine(Path.Combine(Path.GetTempPath(), "Unity3dRider"), DateTime.Now.ToString("yyyy-MM-ddT-HH-mm-ss") + ".log");
    
    private static bool Initialized;
    private static string SlnFile;
    private static readonly ILog Logger = Log.GetLog("RiderPlugin");
    private static RiderProtocolController ourRiderProtocolController;

    public static LoggingLevel SelectedLoggingLevel { get; private set; }

    public static LoggingLevel SelectedLoggingLevelMainThread
    {
      get { return (LoggingLevel) EditorPrefs.GetInt("Rider_SelectedLoggingLevel", 1); }
      set
      {
        SelectedLoggingLevel = value;
        EditorPrefs.SetInt("Rider_SelectedLoggingLevel", (int) value);
      }
    }
    
    public static bool SendConsoleToRider
    {
      get{return EditorPrefs.GetBool("Rider_SendConsoleToRider", false);}
      set{EditorPrefs.SetBool("Rider_SendConsoleToRider", value);}
    }
    
    public static bool Enabled
    {
      get
      {
        var defaultApp = GetExternalScriptEditor();
        return !string.IsNullOrEmpty(defaultApp) && Path.GetFileName(defaultApp).ToLower().Contains("rider");
      }
    }
    
    public static string GetExternalScriptEditor()
    {
      return EditorPrefs.GetString("kScriptsDefaultApp");
    }

    public static void SetExternalScriptEditor(string path)
    {
      EditorPrefs.SetString("kScriptsDefaultApp", path);
    }
    
    
    private static string GetDefaultApp()
    {
      var allFoundPaths = GetAllRiderPaths().Select(a=>new FileInfo(a).FullName).ToArray();
      var alreadySetPath = new FileInfo(GetExternalScriptEditor()).FullName;
      
      if (!string.IsNullOrEmpty(alreadySetPath) && RiderPathExist(alreadySetPath) && !allFoundPaths.Any() ||
          !string.IsNullOrEmpty(alreadySetPath) && RiderPathExist(alreadySetPath) && allFoundPaths.Any() &&
          allFoundPaths.Contains(alreadySetPath))
      {
        RiderPath = alreadySetPath;
      }
      else if (allFoundPaths.Contains(new FileInfo(RiderPath).FullName)) {}
      else
      RiderPath = allFoundPaths.FirstOrDefault();

      return RiderPath;
    }

    private static string[] GetAllRiderPaths()
    {
      switch (SystemInfoRiderPlugin.operatingSystemFamily)
      {
        case OperatingSystemFamily.Windows:
          string[] folders =
          {
            @"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\JetBrains", Path.Combine(
              Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
              @"Microsoft\Windows\Start Menu\Programs\JetBrains Toolbox")
          };

          var newPathLnks = folders.Select(b => new DirectoryInfo(b)).Where(a => a.Exists)
            .SelectMany(c => c.GetFiles("*Rider*.lnk")).ToArray();
          if (newPathLnks.Any())
          {
            var newPaths = newPathLnks
              .Select(newPathLnk => new FileInfo(ShortcutResolver.Resolve(newPathLnk.FullName)))
              .Where(fi => File.Exists(fi.FullName))
              .ToArray()
              .OrderByDescending(fi => FileVersionInfo.GetVersionInfo(fi.FullName).ProductVersion)
              .Select(a => a.FullName).ToArray();

            return newPaths;
          }
          break;

        case OperatingSystemFamily.MacOSX:
          // "/Applications/*Rider*.app"
          //"~/Applications/JetBrains Toolbox/*Rider*.app"
          string[] foldersMac =
          {
            "/Applications", Path.Combine(Environment.GetEnvironmentVariable("HOME"), "Applications/JetBrains Toolbox")
          };
          var newPathsMac = foldersMac.Select(b => new DirectoryInfo(b)).Where(a => a.Exists)
            .SelectMany(c => c.GetDirectories("*Rider*.app"))
            .Select(a => a.FullName).ToArray();
          return newPathsMac;
      }
      return new string[0];
    }

    private static string GetTargetFrameworkVersionDefault(string defaultValue)
    {
      if (SystemInfoRiderPlugin.operatingSystemFamily == OperatingSystemFamily.Windows)
      {
        var dir = new DirectoryInfo(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework");
        if (dir.Exists)
        {
          var availableVersions = dir.GetDirectories("v*").Select(a => a.Name.Substring(1))
            .Where(v => TryCatch(v, s => { })).ToArray();
          if (availableVersions.Any() && !availableVersions.Contains(defaultValue))
          {
            defaultValue = availableVersions.OrderBy(a => new Version(a)).Last();
          }
        }
      }
      return defaultValue;
    }


    public static string TargetFrameworkVersion
    {
      get
      {
        return EditorPrefs.GetString("Rider_TargetFrameworkVersion", GetTargetFrameworkVersionDefault("4.6"));
      }
      set
      {
        TryCatch(value, val =>
        {
          EditorPrefs.SetString("Rider_TargetFrameworkVersion", val);
        });
      }
    }
    
    public static string TargetFrameworkVersionOldMono
    {
      get
      {
        return EditorPrefs.GetString("Rider_TargetFrameworkVersionOldMono", GetTargetFrameworkVersionDefault("3.5"));
      }
      set
      {
        TryCatch(value, val =>
        {
          EditorPrefs.SetString("Rider_TargetFrameworkVersionOldMono", val);
        });
      }
    }

    private static bool TryCatch(string value, Action<string> action)
    {
      try
      {
        new Version(value); // mono 2.6 doesn't support Version.TryParse
        action(value);
        return true;
      }
      catch (ArgumentException)
      {
      } // can't put loggin here because ot fire on every symbol
      catch (FormatException)
      {
      }
      return false;
    }

    public static string RiderPath
    {
      get { return EditorPrefs.GetString("Rider_RiderPath", GetAllRiderPaths().FirstOrDefault()); }
      set { EditorPrefs.SetString("Rider_RiderPath", value); }
    }
    
    public static bool RiderInitializedOnce
    {
      get { return EditorPrefs.GetBool("RiderInitializedOnce", false); }
      set { EditorPrefs.SetBool("RiderInitializedOnce", value); }
    }

    private static void InitRiderPlugin()
    {
      SelectedLoggingLevel = SelectedLoggingLevelMainThread;
      
      var projectDirectory = Directory.GetParent(Application.dataPath).FullName;

      var projectName = Path.GetFileName(projectDirectory);
      SlnFile = Path.Combine(projectDirectory, string.Format("{0}.sln", projectName));

      InitializeEditorInstanceJson(projectDirectory);
      
      RiderAssetPostprocessor.OnGeneratedCSProjectFiles(); // for the case when files were changed and user just alt+tab to unity to make update, we want to fire

      ourRiderProtocolController = new RiderProtocolController(new RiderLogger(), Application.dataPath, 
        new MainThreadDispatcher(), 
        play=>{EditorApplication.isPlaying = play;}, 
        ()=>
      {
        AssetDatabase.Refresh();
      });
      UnityLogRegisterCallBack();
      Initialized = true;
      Debug.Log(string.Format("Rider plugin initialized. Further logs in: {0}", logPath));
    }

    private static void AddRiderToRecentlyUsedScriptApp(string userAppPath, string recentAppsKey)
    {
      for (int index = 0; index < 10; ++index)
      {
        string path = EditorPrefs.GetString(recentAppsKey + (object) index);
        if (File.Exists(path) && Path.GetFileName(path).ToLower().Contains("rider"))
          return;
      }
      EditorPrefs.SetString(recentAppsKey + 9, userAppPath);
    }

    private static bool RiderPathExist(string path)
    {
      if (string.IsNullOrEmpty(path))
        return false;
      // windows or mac
      var fileInfo = new FileInfo(path);
      if (!fileInfo.Name.ToLower().Contains("rider"))
        return false;
      var directoryInfo = new DirectoryInfo(path);
      return fileInfo.Exists || (SystemInfoRiderPlugin.operatingSystemFamily == OperatingSystemFamily.MacOSX &&
                                 directoryInfo.Exists);
    }

    /// <summary>
    /// Creates and deletes Library/EditorInstance.json containing version and process ID
    /// </summary>
    /// <param name="projectDirectory">Path to the project root directory</param>
    private static void InitializeEditorInstanceJson(string projectDirectory)
    {
      // Only manage EditorInstance.json for 4.x and 5.x - it's a native feature for 2017.x
#if !UNITY_2017_1_OR_NEWER
      Logger.Verbose("Writing Library/EditorInstance.json");

      var library = Path.Combine(projectDirectory, "Library");
      var editorInstanceJsonPath = Path.Combine(library, "EditorInstance.json");

      File.WriteAllText(editorInstanceJsonPath, string.Format(@"{{
  ""process_id"": {0},
  ""version"": ""{1}""
}}", Process.GetCurrentProcess().Id, Application.unityVersion));

      AppDomain.CurrentDomain.DomainUnload += (sender, args) =>
      {
        Logger.Verbose("Deleting Library/EditorInstance.json");
        File.Delete(editorInstanceJsonPath);
      };
#endif
    }

    /// <summary>
    /// Asset Open Callback (from Unity)
    /// </summary>
    /// <remarks>
    /// Called when Unity is about to open an asset.
    /// </remarks>
    [UnityEditor.Callbacks.OnOpenAssetAttribute()]
    static bool OnOpenedAsset(int instanceID, int line)
    {
      if (!Enabled) 
        return false;
      if (!Initialized)
      {
        // make sure the plugin was initialized first.
        // this can happen in case "Rider" was set as the default scripting app only after this plugin was imported.
        InitRiderPlugin();
      }

      string appPath = Path.GetDirectoryName(Application.dataPath);

      // determine asset that has been double clicked in the project view
      var selected = EditorUtility.InstanceIDToObject(instanceID);

      var assetFilePath = Path.Combine(appPath, AssetDatabase.GetAssetPath(selected));
      if (!(selected.GetType().ToString() == "UnityEditor.MonoScript" ||
            selected.GetType().ToString() == "UnityEngine.Shader" ||
            (selected.GetType().ToString() == "UnityEngine.TextAsset" &&
#if UNITY_5 || UNITY_5_5_OR_NEWER
             EditorSettings.projectGenerationUserExtensions.Contains(Path.GetExtension(assetFilePath).Substring(1))
#else
            EditorSettings.externalVersionControl.Contains(Path.GetExtension(assetFilePath).Substring(1))
#endif
            )))
        return false;

      SyncSolution(); // added to handle opening file, which was just recently created.

      if (ourRiderProtocolController.Model!=null)
      {
        var connected = false;
        try
        {
          // HostConnected also means that in Rider and in Unity the same solution is opened
          connected = ourRiderProtocolController.Model.IsClientConnected.Sync(RdVoid.Instance,
            new RpcTimeouts(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(200)));
        }
        catch (Exception)
        {
          Logger.Verbose("Rider Protocol not connected.");
        }
        if (connected)
        {
          int col = 0;
          Logger.Verbose("Calling OpenFileLineCol: {0}, {1}, {2}", assetFilePath, line, col);
          //var task = 
          ourRiderProtocolController.Model.OpenFileLineCol.Start(new RdOpenFileArgs(assetFilePath, line, col));
          ActivateWindow();
          //task.Result.Advise(); todo: fallback to CallRider, if returns false
          return true;
        }
      }

      var args = string.Format("{0}{1}{0} --line {2} {0}{3}{0}", "\"", SlnFile, line, assetFilePath);
      return CallRider(args);

    }
    
    private static bool CallRider(string args)
    {
      var defaultApp = GetDefaultApp();
      if (!RiderPathExist(defaultApp))
      {
        return false;
      }

      var proc = new Process();
      if (SystemInfoRiderPlugin.operatingSystemFamily == OperatingSystemFamily.MacOSX)
      {
        proc.StartInfo.FileName = "open";
        proc.StartInfo.Arguments = string.Format("-n {0}{1}{0} --args {2}", "\"", "/" + defaultApp, args);
        Logger.Verbose("{0} {1}", proc.StartInfo.FileName, proc.StartInfo.Arguments);
      }
      else
      {
        proc.StartInfo.FileName = defaultApp;
        proc.StartInfo.Arguments = args;
        Logger.Verbose("{2}{0}{2}" + " {1}", proc.StartInfo.FileName, proc.StartInfo.Arguments, "\"");
      }

      proc.StartInfo.UseShellExecute = false;
      proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
      proc.StartInfo.CreateNoWindow = true;
      proc.StartInfo.RedirectStandardOutput = true;
      proc.Start();

      ActivateWindow();
      return true;
    }

    private static void ActivateWindow()
    {
      if (SystemInfoRiderPlugin.operatingSystemFamily == OperatingSystemFamily.Windows)
      {
        try
        {
          var process = GetRiderProcess();
          if (process != null)
          {
            // Collect top level windows
            var topLevelWindows = User32Dll.GetTopLevelWindowHandles();
            // Get process main window title
            var windowHandle = topLevelWindows.FirstOrDefault(hwnd => User32Dll.GetWindowProcessId(hwnd) == process.Id);
            Logger.Verbose("ActivateWindow: {0} {1}", process.Id, windowHandle);
            if (windowHandle != IntPtr.Zero)
            {
              //User32Dll.ShowWindow(windowHandle, 9); //SW_RESTORE = 9
              User32Dll.SetForegroundWindow(windowHandle);
            }
          }
        }
        catch (Exception e)
        {
          Logger.Warn("Exception on ActivateWindow: " + e);
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
    
    private static void UnityLogRegisterCallBack()
    {
      var eventInfo = typeof(Application).GetEvent("logMessageReceived", BindingFlags.Static | BindingFlags.Public);
      if (eventInfo != null)
      {
        eventInfo.AddEventHandler(null, new Application.LogCallback(ApplicationOnLogMessageReceived));
        AppDomain.CurrentDomain.DomainUnload += (EventHandler) ((_, __) =>
        {
          eventInfo.RemoveEventHandler(null, new Application.LogCallback(ApplicationOnLogMessageReceived));
        });
      }
      else
      {
#pragma warning disable 612, 618
        Application.RegisterLogCallback(ApplicationOnLogMessageReceived);
#pragma warning restore 612, 618
        
      }
    }
    
    private static void ApplicationOnLogMessageReceived(string message, string stackTrace, LogType type)
    {
      if (SendConsoleToRider)
      {
        if (ourRiderProtocolController == null)
          return;
        if (ourRiderProtocolController.myProtocol == null)
          return;
        // use Protocol to pass log entries to Rider
        ourRiderProtocolController.myProtocol.Scheduler.InvokeOrQueue(() =>
        {
          if (ourRiderProtocolController.Model != null)
          {
            switch (type)
            {
              case LogType.Error:
              case LogType.Exception:
                SentLogEvent(message, stackTrace, RdLogEventType.Error);
                break;
              case LogType.Warning:
                SentLogEvent(message, stackTrace, RdLogEventType.Warning);
                break;
              default:
                SentLogEvent(message, stackTrace, RdLogEventType.Message);
                break;
            }
          }
        });
      }
    }

    private static void SentLogEvent(string message, string stackTrace, RdLogEventType type)
    {
      if (!message.StartsWith("[Rider][TRACE]")) // avoid sending because in Trace mode log about sending log event to Rider, will also appear in unity log
        ourRiderProtocolController.Model.LogModelInitialized.Value.Log.Fire(new RdLogEvent(type, message, stackTrace));
    }

    // The default "Open C# Project" menu item will use the external script editor to load the .sln
    // file, but unless Unity knows the external script editor can properly load solutions, it will
    // also launch MonoDevelop (or the OS registered app for .sln files). This menu item side steps
    // that issue, and opens the solution in Rider without opening MonoDevelop as well.
    // Unity 2017.1 and later recognise Rider as an app that can load solutions, so this menu isn't
    // needed in newer versions.
    [MenuItem("Assets/Open C# Project in Rider", false, 1000)]
    static void MenuOpenProject()
    {
      // Force the project files to be sync
      SyncSolution();

      // Load Project
      CallRider(string.Format("{0}{1}{0}", "\"", SlnFile));
    }

    [MenuItem("Assets/Open C# Project in Rider", true, 1000)]
    static bool ValidateMenuOpenProject()
    {
      return Enabled;
    }

    /// <summary>
    /// Force Unity To Write Project File
    /// </summary>
    private static void SyncSolution()
    {
      System.Type T = System.Type.GetType("UnityEditor.SyncVS,UnityEditor");
      System.Reflection.MethodInfo SyncSolution = T.GetMethod("SyncSolution",
        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
      SyncSolution.Invoke(null, null);
    }

    /// <summary>
    /// JetBrains Rider Integration Preferences Item
    /// </summary>
    /// <remarks>
    /// Contains all 3 toggles: Enable/Disable; Debug On/Off; Writing Launch File On/Off
    /// </remarks>
    [PreferenceItem("Rider")]
    static void RiderPreferencesItem()
    {
      EditorGUILayout.BeginVertical();
      EditorGUI.BeginChangeCheck();

      var alternatives = GetAllRiderPaths();
      if (alternatives.Any())
      {
        int index = Array.IndexOf(alternatives, RiderPath);
        var alts = alternatives.Select(s => s.Replace("/", ":"))
          .ToArray(); // hack around https://fogbugz.unity3d.com/default.asp?940857_tirhinhe3144t4vn
        RiderPath = alternatives[EditorGUILayout.Popup("Rider executable:", index == -1 ? 0 : index, alts)];
        if (EditorGUILayout.Toggle(new GUIContent("Rider is default editor"), Enabled))
        {
          SetExternalScriptEditor(RiderPath);
          EditorGUILayout.HelpBox("Unckecking will restore default external editor.", MessageType.None);
        }
        else
        {
          SetExternalScriptEditor(string.Empty);
          EditorGUILayout.HelpBox("Checking will set Rider as default external editor", MessageType.None);
        }
      }

      GUILayout.BeginVertical();
      string status = "TargetFrameworkVersion for Runtime";
      EditorGUILayout.TextArea(status, EditorStyles.boldLabel);
        var help = @"TargetFramework >= 4.5 is recommended.";
        TargetFrameworkVersion =
          EditorGUILayout.TextField(
            new GUIContent("NET 4.6",
              help), TargetFrameworkVersion);
        EditorGUILayout.HelpBox(help, MessageType.None);  
        var helpOldMono = @"TargetFramework = 3.5 is recommended.
 - With 4.5 Rider may show ambiguous references in UniRx.";

        TargetFrameworkVersionOldMono =
          EditorGUILayout.TextField(
            new GUIContent("NET 3.5",
              helpOldMono), TargetFrameworkVersionOldMono);
        EditorGUILayout.HelpBox(helpOldMono, MessageType.None);
      
      GUILayout.EndVertical();

      EditorGUI.EndChangeCheck();

      EditorGUI.BeginChangeCheck();

      var loggingMsg =
        @"Sets the amount of Rider Debug output. If you are about to report an issue, please select Verbose logging level and attach Unity console output to the issue.";
      SelectedLoggingLevelMainThread = (LoggingLevel) EditorGUILayout.EnumPopup(new GUIContent("Logging Level", loggingMsg), SelectedLoggingLevelMainThread);
      EditorGUILayout.HelpBox(loggingMsg, MessageType.None);

      SendConsoleToRider =
        EditorGUILayout.Toggle(
          new GUIContent("Send output from Unity to Rider.",
            help), SendConsoleToRider);
      
      EditorGUI.EndChangeCheck();

      var url = "https://github.com/JetBrains/resharper-unity";
      LinkButton(url, url);

/*      if (GUILayout.Button("reset RiderInitializedOnce = false"))
      {
        RiderInitializedOnce = false;
      }*/
      
      EditorGUILayout.EndVertical();
    }

    private static void LinkButton(string caption, string url)
    {
      var style = GUI.skin.label;
      style.richText = true;
      caption = string.Format("<color=#0000FF>{0}</color>", caption);

      bool bClicked = GUILayout.Button(caption, style);

      var rect = GUILayoutUtility.GetLastRect();
      rect.width = style.CalcSize(new GUIContent(caption)).x;
      EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

      if (bClicked)
        Application.OpenURL(url);
    }

    #region SystemInfoRiderPlugin

    private static class SystemInfoRiderPlugin
    {
      public static OperatingSystemFamily operatingSystemFamily
      {
        get
        {
#if UNITY_5_5_OR_NEWER
return SystemInfo.operatingSystemFamily;
#else
          if (SystemInfo.operatingSystem.StartsWith("Mac", StringComparison.InvariantCultureIgnoreCase))
          {
            return OperatingSystemFamily.MacOSX;
          }
          if (SystemInfo.operatingSystem.StartsWith("Win", StringComparison.InvariantCultureIgnoreCase))
          {
            return OperatingSystemFamily.Windows;
          }
          if (SystemInfo.operatingSystem.StartsWith("Lin", StringComparison.InvariantCultureIgnoreCase))
          {
            return OperatingSystemFamily.Linux;
          }
          return OperatingSystemFamily.Other;
#endif
        }
      }
    }
#if !UNITY_5_5_OR_NEWER
    enum OperatingSystemFamily
    {
      Other,
      MacOSX,
      Windows,
      Linux,
    }
#endif
    #endregion
    
    class RiderLogger : ILog
    {
      public bool IsEnabled(LoggingLevel level)
      {
        return level <= RiderPlugin.SelectedLoggingLevel;
      }

      public void Log(LoggingLevel level, string message, Exception exception = null)
      {
        if (!IsEnabled(level))
          return;

        var text = "[Rider][" + level + "]" + DateTime.Now.ToString("HH:mm:ss:ff") + " " + message;

        // using Unity logs causes frequent Unity hangs
        File.AppendAllText(RiderPlugin.logPath,Environment.NewLine + text);
//      switch (level)
//      {
//        case LoggingLevel.FATAL:
//        case LoggingLevel.ERROR:
//          Debug.LogError(text);
//          if (exception != null)
//            Debug.LogException(exception);
//          break;
//        case LoggingLevel.WARN:
//          Debug.LogWarning(text);
//          if (exception != null)
//            Debug.LogException(exception);
//          break;
//        case LoggingLevel.INFO:
//        case LoggingLevel.VERBOSE:
//          Debug.Log(text);
//          if (exception != null)
//            Debug.LogException(exception);
//          break;
//        default:
//          break;
//      }
      }

      public string Category { get; private set; }
    }
    
    class MainThreadDispatcher : IScheduler
    {
      public MainThreadDispatcher()
      {
        EditorApplication.update += DispatchTasks;
      }
    
      /// <summary>
      /// The queue of tasks that are being requested for the next time DispatchTasks is called
      /// </summary>
      private readonly Queue<Action> myTaskQueue = new Queue<Action>();

      /// <summary>
      /// Dispatches the specified action delegate.
      /// </summary>
      /// <param name="action">Action  being requested</param>
      public void Queue(Action action)
      {
        lock (myTaskQueue)
        {
          myTaskQueue.Enqueue(action);
        }
      }

      /// <summary>
      /// Dispatches the tasks that has been requested since the last call to this function
      /// </summary>
      private void DispatchTasks()
      {
        if (IsActive)
        {
          lock (myTaskQueue)
          {
            foreach (Action task in myTaskQueue)
            {
              task();
            }

            myTaskQueue.Clear();
          }
        }
      }

      /// <summary>
      /// Indicates whether there are tasks available for dispatching
      /// </summary>
      /// <value>
      /// <c>true</c> if there are tasks available for dispatching; otherwise, <c>false</c>.
      /// </value>
      public bool IsActive
      {
        get { return myTaskQueue.Count > 0; }
      }
      public bool OutOfOrderExecution
      {
        get { return false; }
      }
    }
  }
}

// Developed using JetBrains Rider =)