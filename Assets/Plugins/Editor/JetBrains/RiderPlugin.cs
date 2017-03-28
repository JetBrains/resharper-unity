using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Plugins.Editor.JetBrains
{
  [InitializeOnLoad]
  public static class RiderPlugin
  {
    private static bool Initialized;

    private static string SlnFile;

    private static string DefaultApp
    {
      get { return EditorPrefs.GetString("kScriptsDefaultApp"); }
    }

    public static bool TargetFrameworkVersion45
    {
      get { return EditorPrefs.GetBool("Rider_TargetFrameworkVersion45", true); }
      set { EditorPrefs.SetBool("Rider_TargetFrameworkVersion45", value); }
    }

    internal static bool Enabled
    {
      get
      {
        return !string.IsNullOrEmpty(DefaultApp) && DefaultApp.ToLower().Contains("rider");
      }
    }

    static RiderPlugin()
    {
      if (Enabled)
      {
        InitRiderPlugin();
      }
    }

    private static void InitRiderPlugin()
    {
      var riderFileInfo = new FileInfo(DefaultApp);

      var newPath = riderFileInfo.FullName;
      // try to search the new version

      if (!riderFileInfo.Exists)
      {
        switch (riderFileInfo.Extension)
        {
          case ".exe":
          {
            var possibleNew =
              riderFileInfo.Directory.Parent.Parent.GetDirectories("*ider*")
                .SelectMany(a => a.GetDirectories("bin"))
                .SelectMany(a => a.GetFiles(riderFileInfo.Name))
                .ToArray();
            if (possibleNew.Length > 0)
              newPath = possibleNew.OrderBy(a => a.LastWriteTime).Last().FullName;
            break;
          }
        }
        if (newPath != riderFileInfo.FullName)
        {
          Log(string.Format("Update {0} to {1}", riderFileInfo.FullName, newPath));
          EditorPrefs.SetString("kScriptsDefaultApp", newPath);
        }
      }

      var projectDirectory = Directory.GetParent(Application.dataPath).FullName;
      var projectName = Path.GetFileName(projectDirectory);
      SlnFile = Path.Combine(projectDirectory, string.Format("{0}.sln", projectName));
      UpdateUnitySettings(SlnFile);

      Initialized = true;
    }

    /// <summary>
    /// Helps to open xml and txt files at least on Windows
    /// </summary>
    /// <param name="slnFile"></param>
    private static void UpdateUnitySettings(string slnFile)
    {
      try
      {
        EditorPrefs.SetString("kScriptEditorArgs", string.Format("{0}{1}{0} {0}$(File){0}", "\"", slnFile));
      }
      catch (Exception e)
      {
        Log("Exception on updating kScriptEditorArgs: " + e.Message);
      }
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
      if (Enabled)
      {
        if (!Initialized)
        {
          // make sure the plugin was initialized first.
          // this can happen in case "Rider" was set as the default scripting app only after this plugin was imported.
          InitRiderPlugin();
          RiderAssetPostprocessor.OnGeneratedCSProjectFiles();
        }

        string appPath = Path.GetDirectoryName(Application.dataPath);

        // determine asset that has been double clicked in the project view
        var selected = EditorUtility.InstanceIDToObject(instanceID);

        if (selected.GetType().ToString() == "UnityEditor.MonoScript" ||
            selected.GetType().ToString() == "UnityEngine.Shader")
        {
          SyncSolution(); // added to handle opening file, which was just recently created.
          var assetFilePath = Path.Combine(appPath, AssetDatabase.GetAssetPath(selected));
          if (!CallUDPRider(line, SlnFile, assetFilePath))
          {
              var args = string.Format("{0}{1}{0} -l {2} {0}{3}{0}", "\"", SlnFile, line, assetFilePath);
              CallRider(DefaultApp, args);
          }
          return true;
        }
      }
      return false;
    }

    private static bool CallUDPRider(int line, string slnPath, string filePath)
    {
      Log(string.Format("CallUDPRider({0} {1} {2})", line, slnPath, filePath));
      using (var sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
      {
        try
        {
          sock.ReceiveTimeout = 10000;

          var serverAddr = IPAddress.Parse("127.0.0.1");
          var endPoint = new IPEndPoint(serverAddr, 11234);

          var text = line + "\r\n" + slnPath + "\r\n" + filePath + "\r\n";
          var send_buffer = Encoding.ASCII.GetBytes(text);
          sock.SendTo(send_buffer, endPoint);

          var rcv_buffer = new byte[1024];

          // Poll the socket for reception with a 10 ms timeout.
          if (!sock.Poll(10000, SelectMode.SelectRead))
          {
            throw new TimeoutException();
          }

          int bytesRec = sock.Receive(rcv_buffer); // This call will not block
          string status = Encoding.ASCII.GetString(rcv_buffer, 0, bytesRec);
          if (status == "ok")
          {
            ActivateWindow(new FileInfo(DefaultApp).FullName);
            return true;
          }
        }
        catch (Exception)
        {
          //error Timed out
          Log("Socket error or no response. Have you installed RiderUnity3DConnector in Rider?");
        }
      }
      return false;
    }

    private static void CallRider(string riderPath, string args)
    {
      var riderFileInfo = new FileInfo(riderPath);
      var macOSVersion = riderFileInfo.Extension == ".app";
      var riderExists = macOSVersion ? new DirectoryInfo(riderPath).Exists : riderFileInfo.Exists;
      
      if (!riderExists)
      {
        EditorUtility.DisplayDialog("Rider executable not found", "Please update 'External Script Editor' path to JetBrains Rider.", "OK");
      }

      var proc = new Process();
      if (macOSVersion)
      {
        proc.StartInfo.FileName = "open";
        proc.StartInfo.Arguments = string.Format("-n {0}{1}{0} --args {2}", "\"", "/" + riderPath, args);
        Log(proc.StartInfo.FileName + " " + proc.StartInfo.Arguments);
      }
      else
      {
        proc.StartInfo.FileName = riderPath;
        proc.StartInfo.Arguments = args;
        Log("\"" + proc.StartInfo.FileName + "\"" + " " + proc.StartInfo.Arguments);
      }

      proc.StartInfo.UseShellExecute = false;
      proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
      proc.StartInfo.CreateNoWindow = true;
      proc.StartInfo.RedirectStandardOutput = true;
      proc.Start();

      ActivateWindow(riderPath);
    }

    private static void ActivateWindow(string riderPath)
    {
      if (new FileInfo(riderPath).Extension == ".exe")
      {
        try
        {
          var process = Process.GetProcesses().FirstOrDefault(p =>
          {
            string processName;
            try
            {
              processName = p.ProcessName; // some processes like kaspersky antivirus throw exception on attempt to get ProcessName
            }
            catch (Exception)
            {
              return false;
            }

            return !p.HasExited && processName.ToLower().Contains("rider");
          });
          if (process != null)
          {
            // Collect top level windows
            var topLevelWindows = User32Dll.GetTopLevelWindowHandles();
            // Get process main window title
            var windowHandle = topLevelWindows.FirstOrDefault(hwnd => User32Dll.GetWindowProcessId(hwnd) == process.Id);
            if (windowHandle != IntPtr.Zero)
              User32Dll.SetForegroundWindow(windowHandle);
          }
        }
        catch (Exception e)
        {
          Log("Exception on ActivateWindow: " + e);
        }
      }
    }

    [MenuItem("Assets/Open C# Project in Rider", false, 1000)]
    static void MenuOpenProject()
    {
      // Force the project files to be sync
      SyncSolution();

      // Load Project
      CallRider(DefaultApp, string.Format("{0}{1}{0}", "\"", SlnFile));
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

    public static void Log(object message)
    {
      Debug.Log("[Rider] " + message);
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

      var url = "https://github.com/JetBrains/Unity3dRider";
      if (GUILayout.Button(url))
      {
        Application.OpenURL(url);
      }

      EditorGUI.BeginChangeCheck();

      var help = @"For now target 4.5 is strongly recommended.
 - Without 4.5:
    - Rider will fail to resolve System.Linq on Mac/Linux
    - Rider will fail to resolve Firebase Analytics.
 - With 4.5 Rider will show ambiguous references in UniRx.
All those problems will go away after Unity upgrades to mono4.";
      TargetFrameworkVersion45 =
        EditorGUILayout.Toggle(
          new GUIContent("TargetFrameworkVersion 4.5",
            help), TargetFrameworkVersion45);
      EditorGUILayout.HelpBox(help, MessageType.None);

      EditorGUI.EndChangeCheck();
    }

    static class User32Dll
    {

      /// <summary>
      /// Gets the ID of the process that owns the window.
      /// Note that creating a <see cref="Process"/> wrapper for that is very expensive because it causes an enumeration of all the system processes to happen.
      /// </summary>
      public static int GetWindowProcessId(IntPtr hwnd)
      {
        uint dwProcessId;
        GetWindowThreadProcessId(hwnd, out dwProcessId);
        return unchecked((int) dwProcessId);
      }

      /// <summary>
      /// Lists the handles of all the top-level windows currently available in the system.
      /// </summary>
      public static List<IntPtr> GetTopLevelWindowHandles()
      {
        var retval = new List<IntPtr>();
        EnumWindowsProc callback = (hwnd, param) =>
        {
          retval.Add(hwnd);
          return 1;
        };
        EnumWindows(Marshal.GetFunctionPointerForDelegate(callback), IntPtr.Zero);
        GC.KeepAlive(callback);
        return retval;
      }

      public delegate Int32 EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

      [DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
      public static extern Int32 EnumWindows(IntPtr lpEnumFunc, IntPtr lParam);

      [DllImport("user32.dll", SetLastError = true)]
      static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

      [DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
      public static extern Int32 SetForegroundWindow(IntPtr hWnd);
    }
  }
}

// Developed using JetBrains Rider =)
