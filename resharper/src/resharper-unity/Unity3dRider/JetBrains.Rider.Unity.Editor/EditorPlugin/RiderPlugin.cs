using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JetBrains.DataFlow;
using JetBrains.Platform.RdFramework;
using JetBrains.Platform.RdFramework.Tasks;
using JetBrains.Platform.Unity.Model;
using JetBrains.Util.Logging;
using UnityEditor;
using Application = UnityEngine.Application;
using Debug = UnityEngine.Debug;

namespace JetBrains.Rider.Unity.Editor
{
  [InitializeOnLoad]
  public static class RiderPlugin
  {
    private static IPluginSettings ourPluginSettings;
    private static RiderApplication ourRiderApplication;
    
    static RiderPlugin()
    {
      ourPluginSettings = new PluginSettings();
      ourRiderApplication = new RiderApplication(ourPluginSettings);
      var riderPath = ourRiderApplication.GetDefaultRiderApp(UnityApplication.GetExternalScriptEditor(), RiderApplication.GetAllFoundPaths(ourPluginSettings.OperatingSystemFamilyRider));
      if (string.IsNullOrEmpty(riderPath))
        return;

      UnityApplication.AddRiderToRecentlyUsedScriptApp(riderPath, "RecentlyUsedScriptApp");
      if (!PluginSettings.RiderInitializedOnce)
      {
        UnityApplication.SetExternalScriptEditor(riderPath);
        PluginSettings.RiderInitializedOnce = true;
      }

      if (Enabled)
      {
        InitRiderPlugin();
      }
    }

    private static bool ourInitialized;
    internal static string SlnFile;
    private static readonly ILog Logger = Log.GetLog("RiderPlugin");
    private static RiderProtocolController ourRiderProtocolController;

    public static bool Enabled
    {
      get
      {
        var defaultApp = UnityApplication.GetExternalScriptEditor();
        return !string.IsNullOrEmpty(defaultApp) && Path.GetFileName(defaultApp).ToLower().Contains("rider");
      }
    }

    private static void InitRiderPlugin()
    {
      PluginSettings.SelectedLoggingLevel = PluginSettings.SelectedLoggingLevelMainThread;

      var projectDirectory = Directory.GetParent(Application.dataPath).FullName;

      var projectName = Path.GetFileName(projectDirectory);
      SlnFile = Path.Combine(projectDirectory, string.Format("{0}.sln", projectName));

      InitializeEditorInstanceJson(projectDirectory);

      RiderAssetPostprocessor
        .OnGeneratedCSProjectFiles(); // for the case when files were changed and user just alt+tab to unity to make update, we want to fire

      Log.DefaultFactory = new RiderLoggerFactory();

      var lifetimeDefinition = Lifetimes.Define(EternalLifetime.Instance);
      var lifetime = lifetimeDefinition.Lifetime;

      AppDomain.CurrentDomain.DomainUnload += (EventHandler) ((_, __) =>
      {
        Logger.Verbose("lifetimeDefinition.Terminate");
        lifetimeDefinition.Terminate();
      });

      Debug.Log(string.Format("Rider plugin initialized. Further logs in: {0}", LogPath));

      ourRiderProtocolController = new RiderProtocolController(
        Application.dataPath,
        MainThreadDispatcher1,
        play => { EditorApplication.isPlaying = play; },
        AssetDatabase.Refresh,
        lifetime
      );

      var application = new UnityApplication(ourRiderProtocolController, MainThreadDispatcher1);
      application.UnityLogRegisterCallBack();
      ourInitialized = true;
    }
    
    internal static readonly string  LogPath = Path.Combine(Path.Combine(Path.GetTempPath(), "Unity3dRider"), DateTime.Now.ToString("yyyy-MM-ddT-HH-mm-ss") + ".log");

    internal static readonly MainThreadDispatcher MainThreadDispatcher1 = new MainThreadDispatcher();

    /// <summary>
    /// Creates and deletes Library/EditorInstance.json containing info about unity instance
    /// </summary>
    /// <param name="projectDirectory">Path to the project root directory</param>
    private static void InitializeEditorInstanceJson(string projectDirectory)
    {
      Logger.Verbose("Writing Library/EditorInstance.json");

      var library = Path.Combine(projectDirectory, "Library");
      var editorInstanceJsonPath = Path.Combine(library, "EditorInstance.json");

      File.WriteAllText(editorInstanceJsonPath, string.Format(@"{{
  ""process_id"": {0},
  ""version"": ""{1}"",
  ""app_path"": ""{2}"",
  ""app_contents_path"": ""{3}"",
  ""attach_allowed"": ""{4}""
}}", Process.GetCurrentProcess().Id, Application.unityVersion,
        EditorApplication.applicationPath, 
        EditorApplication.applicationContentsPath,
        EditorPrefs.GetBool("AllowAttachedDebuggingOfEditor", true)
        ));

      AppDomain.CurrentDomain.DomainUnload += (sender, args) =>
      {
        Logger.Verbose("Deleting Library/EditorInstance.json");
        File.Delete(editorInstanceJsonPath);
      };
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
      if (!ourInitialized)
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
//#i f UNITY_5 || UNITY_5_5_OR_NEWER
//             EditorSettings.projectGenerationUserExtensions.Contains(Path.GetExtension(assetFilePath).Substring(1))
//#e lse
            EditorSettings.externalVersionControl.Contains(Path.GetExtension(assetFilePath).Substring(1))
//#e ndif
            )))
        return false;

      UnityApplication.SyncSolution(); // added to handle opening file, which was just recently created.

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
          ActivateWindow(ourRiderProtocolController.Model.RiderProcessId.Value);
          //task.Result.Advise(); todo: fallback to CallRider, if returns false
          return true;
        }
      }

      var args = string.Format("{0}{1}{0} --line {2} {0}{3}{0}", "\"", SlnFile, line, assetFilePath);
      return CallRider(args);

    }
    
    internal static bool CallRider(string args)
    {
      var defaultApp = ourRiderApplication.GetDefaultRiderApp(UnityApplication.GetExternalScriptEditor(), RiderApplication.GetAllFoundPaths(ourPluginSettings.OperatingSystemFamilyRider));
      if (string.IsNullOrEmpty(defaultApp))
      {
        return false;
      }

      var proc = new Process();
      if (ourPluginSettings.OperatingSystemFamilyRider == OperatingSystemFamilyRider.MacOSX)
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

    // The default "Open C# Project" menu item will use the external script editor to load the .sln
    // file, but unless Unity knows the external script editor can properly load solutions, it will
    // also launch MonoDevelop (or the OS registered app for .sln files). This menu item side steps
    // that issue, and opens the solution in Rider without opening MonoDevelop as well.
    // Unity 2017.1 and later recognise Rider as an app that can load solutions, so this menu isn't
    // needed in newer versions.
  }
}

// Developed with JetBrains Rider =)