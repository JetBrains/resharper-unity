using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using UnityEditor;
using UnityEngine;
using Application = UnityEngine.Application;
using Debug = UnityEngine.Debug;

namespace Plugins.Editor.JetBrains
{
  [InitializeOnLoad]
  public static class RiderPlugin
  {   
    private static bool Initialized;
    private static string SlnFile;

    private static string GetDefaultApp()
    {
        var alreadySetPath = GetExternalScriptEditor();
        if (!string.IsNullOrEmpty(alreadySetPath) && RiderPathExist(alreadySetPath))
          return alreadySetPath;

        switch (SystemInfoRiderPlugin.operatingSystemFamily)
        {
          case OperatingSystemFamily.Windows:
            //"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\JetBrains\*Rider*.lnk"
            //%appdata%\Microsoft\Windows\Start Menu\Programs\JetBrains Toolbox\*Rider*.lnk
            string[] folders = {@"C:\ProgramData\Microsoft\Windows\Start Menu\Programs\JetBrains", Path.Combine(
              Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
              @"Microsoft\Windows\Start Menu\Programs\JetBrains Toolbox")};

            var newPathLnks = folders.Select(b=>new DirectoryInfo(b)).Where(a => a.Exists).SelectMany(c=>c.GetFiles("*Rider*.lnk"));
            if (newPathLnks.Any())
            {
              var newPath = newPathLnks.Select(newPathLnk=> new FileInfo(ShortcutResolver.Resolve(newPathLnk.FullName))).OrderBy(a => FileVersionInfo.GetVersionInfo(a.FullName).ProductVersion).LastOrDefault();
              if (!string.IsNullOrEmpty(newPath.FullName))
              {
                /*if (EnableLogging) Debug.Log("[Rider] " + string.Format("Update {0} to {1} product version: {2}", alreadySetPath, newPath, FileVersionInfo.GetVersionInfo(newPath.FullName).ProductVersion));
                SetExternalScriptEditor(newPath.FullName);*/
                return newPath.FullName;
              }
            }
            break;

          case OperatingSystemFamily.MacOSX:
            // "/Applications/*Rider*.app"
            //"~/Applications/JetBrains Toolbox/*Rider*.app"
            string[] foldersMac = {"/Applications",Path.Combine(Environment.GetEnvironmentVariable("HOME"), "Applications")};
            var newPathMac = foldersMac.Select(b=>new DirectoryInfo(b)).Where(a => a.Exists)
              .SelectMany(c=>c.GetDirectories("*Rider*.app")).OrderBy(a => FileVersionInfo.GetVersionInfo(a.FullName).ProductVersion).LastOrDefault();
            if (newPathMac != null)
            {
              if (!string.IsNullOrEmpty(newPathMac.FullName))
              {
                /*if (EnableLogging) Debug.Log("[Rider] " + string.Format("Update {0} to {1}", alreadySetPath, newPathMac));
                SetExternalScriptEditor(newPathMac.FullName);*/
                return newPathMac.FullName;
              }
            }           
            break;
        }

        var riderPath = GetExternalScriptEditor();
        if (!RiderPathExist(riderPath))
        {
          Debug.Log("[Rider] Rider plugin for Unity is present, but Rider executable was not found. Please update 'External Script Editor'.");
          return null;
        }

        return riderPath;
    }

    public static bool TargetFrameworkVersion45
    {
      get { return EditorPrefs.GetBool("Rider_TargetFrameworkVersion45", true); }
      set { EditorPrefs.SetBool("Rider_TargetFrameworkVersion45", value); }
    }

    public static bool EnableLogging
    {
      get { return EditorPrefs.GetBool("Rider_EnableLogging", false); }
      set { EditorPrefs.SetBool("Rider_EnableLogging", value); }
    }
    
    public static bool RiderInitializedOnce
    {
      get { return EditorPrefs.GetBool("RiderInitializedOnce", false); }
      set { EditorPrefs.SetBool("RiderInitializedOnce", value); }
    }

    internal static bool Enabled
    {
      get
      {
        var defaultApp = GetExternalScriptEditor();
        return !string.IsNullOrEmpty(defaultApp) && Path.GetFileName(defaultApp).ToLower().Contains("rider");
      }
    }

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

    private static void InitRiderPlugin()
    {
      var projectDirectory = Directory.GetParent(Application.dataPath).FullName;

      var projectName = Path.GetFileName(projectDirectory);
      SlnFile = Path.Combine(projectDirectory, string.Format("{0}.sln", projectName));

      InitializeEditorInstanceJson(projectDirectory);

      Debug.Log("[Rider] " + "Rider plugin initialized. You may enabled more Rider Debug output via Preferences -> Rider -> Enable Logging");
      Initialized = true;
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
    
    private static string GetExternalScriptEditor()
    {
      return EditorPrefs.GetString("kScriptsDefaultApp");
    }

    private static void SetExternalScriptEditor(string path)
    {
      EditorPrefs.SetString("kScriptsDefaultApp", path);
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
      return fileInfo.Exists || (SystemInfoRiderPlugin.operatingSystemFamily==OperatingSystemFamily.MacOSX && directoryInfo.Exists);
    }

    /// <summary>
    /// Creates and deletes Library/EditorInstance.json containing version and process ID
    /// </summary>
    /// <param name="projectDirectory">Path to the project root directory</param>
    private static void InitializeEditorInstanceJson(string projectDirectory)
    {
      // Only manage EditorInstance.json for 4.x and 5.x - it's a native feature for 2017.x
#if UNITY_4 || UNITY_5
      if (EnableLogging) Debug.Log("[Rider] " + "Writing Library/EditorInstance.json");

      var library = Path.Combine(projectDirectory, "Library");
      var editorInstanceJsonPath = Path.Combine(library, "EditorInstance.json");

      File.WriteAllText(editorInstanceJsonPath, string.Format(@"{{
  ""process_id"": {0},
  ""version"": ""{1}""
}}", Process.GetCurrentProcess().Id, Application.unityVersion));

      AppDomain.CurrentDomain.DomainUnload += (sender, args) =>
      {
        if (EnableLogging) Debug.Log("[Rider] " + "Deleting Library/EditorInstance.json");
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
        
        var assetFilePath = Path.Combine(appPath, AssetDatabase.GetAssetPath(selected));
        if (!(selected.GetType().ToString() == "UnityEditor.MonoScript" ||
            selected.GetType().ToString() == "UnityEngine.Shader" ||
            (selected.GetType().ToString() == "UnityEngine.TextAsset" && 
             EditorSettings.projectGenerationUserExtensions.Contains(Path.GetExtension(assetFilePath).Substring(1)))
              )) 
          return false;
        
        SyncSolution(); // added to handle opening file, which was just recently created.
        if (!DetectPortAndOpenFile(line, assetFilePath, SystemInfoRiderPlugin.operatingSystemFamily == OperatingSystemFamily.Windows))
        {
          var args = string.Format("{0}{1}{0} --line {2} {0}{3}{0}", "\"", SlnFile, line, assetFilePath);
          return CallRider(args);
        }
        return true;
      }

      return false;
    }


    private static bool DetectPortAndOpenFile(int line, string filePath, bool isWindows)
    {
      var process = GetRiderProcess();
      if (process == null) 
        return false;
      
      int[] ports = Enumerable.Range(63342, 20).ToArray();
      var res = ports.Any(port => 
      {
        var aboutUrl = string.Format("http://localhost:{0}/api/about/", port);
        var aboutUri = new Uri(aboutUrl);

        using (var client = new WebClient())
        {
          client.Headers.Add("origin", string.Format("http://localhost:{0}", port));
          client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";

          try
          {
            var responce = CallHttpApi(aboutUri, client);
            if (responce.ToLower().Contains("rider"))
            {
              return HttpOpenFile(line, filePath, isWindows, port, client);
            }
          }
          catch (Exception e)
          {
            if (EnableLogging) Debug.Log("[Rider] " + "Exception in DetectPortAndOpenFile: " + e);
          }
        }
        return false;
      });
      return res;
    }

    private static bool HttpOpenFile(int line, string filePath, bool isWindows, int port, WebClient client)
    {
      var url = string.Format("http://localhost:{0}/api/file?file={1}{2}", port, filePath,
        line < 0
          ? "&p=0"
          : "&line=" + line); // &p is needed to workaround https://youtrack.jetbrains.com/issue/IDEA-172350
      if (isWindows)
        url = string.Format(@"http://localhost:{0}/api/file/{1}{2}", port, filePath, line < 0 ? "" : ":" + line);

      var uri = new Uri(url);
      if (EnableLogging) Debug.Log("[Rider] " + string.Format("HttpRequestOpenFile({0})", uri.AbsoluteUri));

      CallHttpApi(uri, client);
      ActivateWindow();
      return true;
    }

    private static string CallHttpApi(Uri uri, WebClient client)
    {
      var responseString = client.DownloadString(uri);
      if (EnableLogging) Debug.Log("[Rider] HttpRequestOpenFile response: " + responseString);
      return responseString;
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
        if (EnableLogging) Debug.Log("[Rider] " + proc.StartInfo.FileName + " " + proc.StartInfo.Arguments);
      }
      else
      {
        proc.StartInfo.FileName = defaultApp;
        proc.StartInfo.Arguments = args;
        if (EnableLogging)
          Debug.Log("[Rider] " + ("\"" + proc.StartInfo.FileName + "\"" + " " + proc.StartInfo.Arguments));
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
            Debug.Log("[Rider] ActivateWindow: " + process.Id + " " + windowHandle);
            if (windowHandle != IntPtr.Zero)
            {
              //User32Dll.ShowWindow(windowHandle, 9); //SW_RESTORE = 9
              User32Dll.SetForegroundWindow(windowHandle);
            }
          }
        }
        catch (Exception e)
        {
          Debug.Log("[Rider] " + ("Exception on ActivateWindow: " + e));
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
          processName = p.ProcessName; // some processes like kaspersky antivirus throw exception on attempt to get ProcessName
        }
        catch (Exception)
        {
          return false;
        }

        return !p.HasExited && processName.ToLower().Contains("rider");
      });
      return process;
    }

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

      var url = "https://github.com/JetBrains/resharper-unity";
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

      EditorGUI.BeginChangeCheck();

      var loggingMsg =
        @"Enable logging. If you are about to report an issue, please enable logging and attach Unity console output to the issue.";
      EnableLogging =
        EditorGUILayout.Toggle(
          new GUIContent("Enable Logging",
            loggingMsg), EnableLogging);
      EditorGUILayout.HelpBox(loggingMsg, MessageType.None);

      EditorGUI.EndChangeCheck();
      
/*      if (GUILayout.Button("reset RiderInitializedOnce = false"))
      {
        RiderInitializedOnce = false;
      }*/
      
      EditorGUILayout.EndVertical();
    }

    #region SystemInfoRiderPlugin
    static class SystemInfoRiderPlugin
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

      [DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true,
        ExactSpelling = true)]
      public static extern Int32 EnumWindows(IntPtr lpEnumFunc, IntPtr lParam);

      [DllImport("user32.dll", SetLastError = true)]
      static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

      [DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true,
        ExactSpelling = true)]
      public static extern Int32 SetForegroundWindow(IntPtr hWnd);

      [DllImport("user32.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true,
        ExactSpelling = true)]
      public static extern UInt32 ShowWindow(IntPtr hWnd, Int32 nCmdShow);
    }
    
    static class ShortcutResolver
    {
      #region Signitures imported from http://pinvoke.net

      [DllImport("shfolder.dll", CharSet = CharSet.Auto)]
      internal static extern int SHGetFolderPath(IntPtr hwndOwner, int nFolder, IntPtr hToken, int dwFlags, StringBuilder lpszPath);

      [Flags()]
      enum SLGP_FLAGS
      {
        /// <summary>Retrieves the standard short (8.3 format) file name</summary>
        SLGP_SHORTPATH = 0x1,

        /// <summary>Retrieves the Universal Naming Convention (UNC) path name of the file</summary>
        SLGP_UNCPRIORITY = 0x2,

        /// <summary>Retrieves the raw path name. A raw path is something that might not exist and may include environment variables that need to be expanded</summary>
        SLGP_RAWPATH = 0x4
      }

      [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
      struct WIN32_FIND_DATAW
      {
        public uint dwFileAttributes;
        public long ftCreationTime;
        public long ftLastAccessTime;
        public long ftLastWriteTime;
        public uint nFileSizeHigh;
        public uint nFileSizeLow;
        public uint dwReserved0;
        public uint dwReserved1;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string cFileName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)] public string cAlternateFileName;
      }

      [Flags()]
      enum SLR_FLAGS
      {
        /// <summary>
        /// Do not display a dialog box if the link cannot be resolved. When SLR_NO_UI is set,
        /// the high-order word of fFlags can be set to a time-out value that specifies the
        /// maximum amount of time to be spent resolving the link. The function returns if the
        /// link cannot be resolved within the time-out duration. If the high-order word is set
        /// to zero, the time-out duration will be set to the default value of 3,000 milliseconds
        /// (3 seconds). To specify a value, set the high word of fFlags to the desired time-out
        /// duration, in milliseconds.
        /// </summary>
        SLR_NO_UI = 0x1,

        /// <summary>Obsolete and no longer used</summary>
        SLR_ANY_MATCH = 0x2,

        /// <summary>If the link object has changed, update its path and list of identifiers.
        /// If SLR_UPDATE is set, you do not need to call IPersistFile::IsDirty to determine
        /// whether or not the link object has changed.</summary>
        SLR_UPDATE = 0x4,

        /// <summary>Do not update the link information</summary>
        SLR_NOUPDATE = 0x8,

        /// <summary>Do not execute the search heuristics</summary>
        SLR_NOSEARCH = 0x10,

        /// <summary>Do not use distributed link tracking</summary>
        SLR_NOTRACK = 0x20,

        /// <summary>Disable distributed link tracking. By default, distributed link tracking tracks
        /// removable media across multiple devices based on the volume name. It also uses the
        /// Universal Naming Convention (UNC) path to track remote file systems whose drive letter
        /// has changed. Setting SLR_NOLINKINFO disables both types of tracking.</summary>
        SLR_NOLINKINFO = 0x40,

        /// <summary>Call the Microsoft Windows Installer</summary>
        SLR_INVOKE_MSI = 0x80
      }


      /// <summary>The IShellLink interface allows Shell links to be created, modified, and resolved</summary>
      [ComImport(), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F9-0000-0000-C000-000000000046")]
      interface IShellLinkW
      {
        /// <summary>Retrieves the path and file name of a Shell link object</summary>
        [MethodImpl (MethodImplOptions.InternalCall | MethodImplOptions.PreserveSig, MethodCodeType = MethodCodeType.Runtime)]
        void GetPath([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out WIN32_FIND_DATAW pfd, SLGP_FLAGS fFlags);

        /// <summary>Retrieves the list of item identifiers for a Shell link object</summary>
        [MethodImpl (MethodImplOptions.InternalCall | MethodImplOptions.PreserveSig, MethodCodeType = MethodCodeType.Runtime)]
        void GetIDList(out IntPtr ppidl);

        /// <summary>Sets the pointer to an item identifier list (PIDL) for a Shell link object.</summary>
        [MethodImpl (MethodImplOptions.InternalCall | MethodImplOptions.PreserveSig, MethodCodeType = MethodCodeType.Runtime)]
        void SetIDList(IntPtr pidl);

        /// <summary>Retrieves the description string for a Shell link object</summary>
        [MethodImpl (MethodImplOptions.InternalCall | MethodImplOptions.PreserveSig, MethodCodeType = MethodCodeType.Runtime)]
        void GetDescription([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);

        /// <summary>Sets the description for a Shell link object. The description can be any application-defined string</summary>
        [MethodImpl (MethodImplOptions.InternalCall | MethodImplOptions.PreserveSig, MethodCodeType = MethodCodeType.Runtime)]
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

        /// <summary>Retrieves the name of the working directory for a Shell link object</summary>
        [MethodImpl (MethodImplOptions.InternalCall | MethodImplOptions.PreserveSig, MethodCodeType = MethodCodeType.Runtime)]
        void GetWorkingDirectory([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);

        /// <summary>Sets the name of the working directory for a Shell link object</summary>
        [MethodImpl (MethodImplOptions.InternalCall | MethodImplOptions.PreserveSig, MethodCodeType = MethodCodeType.Runtime)]
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

        /// <summary>Retrieves the command-line arguments associated with a Shell link object</summary>
        [MethodImpl (MethodImplOptions.InternalCall | MethodImplOptions.PreserveSig, MethodCodeType = MethodCodeType.Runtime)]
        void GetArguments([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);

        /// <summary>Sets the command-line arguments for a Shell link object</summary>
        [MethodImpl (MethodImplOptions.InternalCall | MethodImplOptions.PreserveSig, MethodCodeType = MethodCodeType.Runtime)]
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

        /// <summary>Retrieves the hot key for a Shell link object</summary>
        [MethodImpl (MethodImplOptions.InternalCall | MethodImplOptions.PreserveSig, MethodCodeType = MethodCodeType.Runtime)]
        void GetHotkey(out short pwHotkey);

        /// <summary>Sets a hot key for a Shell link object</summary>
        [MethodImpl (MethodImplOptions.InternalCall | MethodImplOptions.PreserveSig, MethodCodeType = MethodCodeType.Runtime)]
        void SetHotkey(short wHotkey);

        /// <summary>Retrieves the show command for a Shell link object</summary>
        [MethodImpl (MethodImplOptions.InternalCall | MethodImplOptions.PreserveSig, MethodCodeType = MethodCodeType.Runtime)]
        void GetShowCmd(out int piShowCmd);

        /// <summary>Sets the show command for a Shell link object. The show command sets the initial show state of the window.</summary>
        [MethodImpl (MethodImplOptions.InternalCall | MethodImplOptions.PreserveSig, MethodCodeType = MethodCodeType.Runtime)]
        void SetShowCmd(int iShowCmd);

        /// <summary>Retrieves the location (path and index) of the icon for a Shell link object</summary>
        [MethodImpl (MethodImplOptions.InternalCall | MethodImplOptions.PreserveSig, MethodCodeType = MethodCodeType.Runtime)]
        void GetIconLocation([Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);

        /// <summary>Sets the location (path and index) of the icon for a Shell link object</summary>
        [MethodImpl (MethodImplOptions.InternalCall | MethodImplOptions.PreserveSig, MethodCodeType = MethodCodeType.Runtime)]
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

        /// <summary>Sets the relative path to the Shell link object</summary>
        [MethodImpl (MethodImplOptions.InternalCall | MethodImplOptions.PreserveSig, MethodCodeType = MethodCodeType.Runtime)]
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);

        /// <summary>Attempts to find the target of a Shell link, even if it has been moved or renamed</summary>
        [MethodImpl (MethodImplOptions.InternalCall | MethodImplOptions.PreserveSig, MethodCodeType = MethodCodeType.Runtime)]
        void Resolve(IntPtr hwnd, SLR_FLAGS fFlags);

        /// <summary>Sets the path and file name of a Shell link object</summary>
        [MethodImpl (MethodImplOptions.InternalCall | MethodImplOptions.PreserveSig, MethodCodeType = MethodCodeType.Runtime)]
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
      }

      [ComImport, Guid("0000010c-0000-0000-c000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
      public interface IPersist
      {
        [MethodImpl (MethodImplOptions.InternalCall | MethodImplOptions.PreserveSig, MethodCodeType = MethodCodeType.Runtime)]
        void GetClassID(out Guid pClassID);
      }


      [ComImport, Guid("0000010b-0000-0000-C000-000000000046"),
       InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
      public interface IPersistFile : IPersist
      {
        [MethodImpl(MethodImplOptions.InternalCall | MethodImplOptions.PreserveSig, MethodCodeType = MethodCodeType.Runtime)]
        new void GetClassID(out Guid pClassID);

        [MethodImpl(MethodImplOptions.InternalCall | MethodImplOptions.PreserveSig, MethodCodeType = MethodCodeType.Runtime)]
        int IsDirty();

        [MethodImpl(MethodImplOptions.InternalCall | MethodImplOptions.PreserveSig, MethodCodeType = MethodCodeType.Runtime)]
        void Load([In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);

        [MethodImpl (MethodImplOptions.InternalCall | MethodImplOptions.PreserveSig, MethodCodeType = MethodCodeType.Runtime)]
        void Save([In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [In, MarshalAs(UnmanagedType.Bool)] bool fRemember);

        [MethodImpl (MethodImplOptions.InternalCall | MethodImplOptions.PreserveSig, MethodCodeType = MethodCodeType.Runtime)]
        void SaveCompleted([In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName);

        [MethodImpl (MethodImplOptions.InternalCall | MethodImplOptions.PreserveSig, MethodCodeType = MethodCodeType.Runtime)]
        void GetCurFile([In, MarshalAs(UnmanagedType.LPWStr)] string ppszFileName);
      }

      const uint STGM_READ = 0;
      const int MAX_PATH = 260;

      // CLSID_ShellLink from ShlGuid.h 
      [
        ComImport(),
        Guid("00021401-0000-0000-C000-000000000046")
      ]
      public class ShellLink
      {
      }

      #endregion

      public static string Resolve(string filename)
      {
        ShellLink link = new ShellLink();
        ((IPersistFile) link).Load(filename, STGM_READ);
        // TODO: if I can get hold of the hwnd call resolve first. This handles moved and renamed files.  
        // ((IShellLinkW)link).Resolve(hwnd, 0) 
        StringBuilder sb = new StringBuilder(MAX_PATH);
        WIN32_FIND_DATAW data = new WIN32_FIND_DATAW();
        ((IShellLinkW) link).GetPath(sb, sb.Capacity, out data, 0);
        return sb.ToString();
      }
    }
  }
}

// Developed using JetBrains Rider =)
