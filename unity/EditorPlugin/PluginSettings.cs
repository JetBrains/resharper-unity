using System;
using System.IO;
using System.Linq;
using JetBrains.DataFlow;
using JetBrains.Util;
using JetBrains.Util.Logging;
using UnityEditor;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor
{
  public interface IPluginSettings
  {
    OperatingSystemFamilyRider OperatingSystemFamilyRider { get; }
    string RiderPath { get; set; }
  }

  public enum AssemblyReloadSettings
  {
    RecompileAndContinuePlaying = 0,
    RecompileAfterFinishedPlaying = 1,
    StopPlayingAndRecompile = 2
  }

  public class PluginSettings : IPluginSettings
  {
    internal static LoggingLevel SelectedLoggingLevel
    {
      get => (LoggingLevel) EditorPrefs.GetInt("Rider_SelectedLoggingLevel", 0);
      private set
      {
        EditorPrefs.SetInt("Rider_SelectedLoggingLevel", (int) value);
        InitLog();
      }
    }

    public static void InitLog()
    {
      if (SelectedLoggingLevel > LoggingLevel.OFF) 
        Log.DefaultFactory = Log.CreateFileLogFactory(EternalLifetime.Instance, PluginEntryPoint.LogPath, true, SelectedLoggingLevel);
      else
        Log.DefaultFactory = new SingletonLogFactory(NullLog.Instance); // use profiler in Unity - this is faster than leaving TextWriterLogFactory with LoggingLevel OFF 
    }

    public static string[] GetInstalledNetFrameworks()
    {
      if (SystemInfoRiderPlugin.operatingSystemFamily != OperatingSystemFamilyRider.Windows)
        throw new InvalidOperationException("GetTargetFrameworkVersionWindowsMono2 is designed for Windows only");

      var programFiles86 = Environment.GetEnvironmentVariable("PROGRAMFILES(X86)") ??
                           Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
      if (string.IsNullOrEmpty(programFiles86))
        programFiles86 = @"C:\Program Files (x86)";
      var referenceAssembliesPath = Path.Combine(programFiles86, @"Reference Assemblies\Microsoft\Framework\.NETFramework");
      var dir = new DirectoryInfo(referenceAssembliesPath);
      if (!dir.Exists)
        return new string[0];

      var availableVersions = dir
        .GetDirectories("v*")
        .Select(a => a.Name.Substring(1))
        .Where(v => InvokeIfValidVersion(v, s => { }))
        .Where(v=>new Version(v) >= new Version("3.5"))
        .ToArray();

      return availableVersions;
    }

    private static bool InvokeIfValidVersion(string input, Action<string> action)
    {
      try
      {
        // ReSharper disable once ObjectCreationAsStatement
        new Version(input); // mono 2.6 doesn't support Version.TryParse
        action(input);
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

    public static bool OverrideTargetFrameworkVersion
    {
      get { return EditorPrefs.GetBool("Rider_OverrideTargetFrameworkVersion", false); }
      private set { EditorPrefs.SetBool("Rider_OverrideTargetFrameworkVersion", value);; }
    }
    
    public static AssemblyReloadSettings AssemblyReloadSettings
    {
      get
      {
        if (UnityUtils.UnityVersion >= new Version(2018, 2))
          return AssemblyReloadSettings.RecompileAndContinuePlaying;
        return (AssemblyReloadSettings) EditorPrefs.GetInt("Rider_AssemblyReloadSettings", (int) AssemblyReloadSettings.RecompileAndContinuePlaying);
      }
      private set { EditorPrefs.SetInt("Rider_AssemblyReloadSettings", (int) value);; }
    }

    private static string TargetFrameworkVersionDefault = "4.6";

    public static string TargetFrameworkVersion
    {
      get { return EditorPrefs.GetString("Rider_TargetFrameworkVersion", TargetFrameworkVersionDefault); }
      private set { InvokeIfValidVersion(value, val => { EditorPrefs.SetString("Rider_TargetFrameworkVersion", val); }); }
    }

    public static bool OverrideTargetFrameworkVersionOldMono
    {
      get { return EditorPrefs.GetBool("Rider_OverrideTargetFrameworkVersionOldMono", false); }
      private set { EditorPrefs.SetBool("Rider_OverrideTargetFrameworkVersionOldMono", value);; }
    }

    private static string TargetFrameworkVersionOldMonoDefault = "3.5";

    public static string TargetFrameworkVersionOldMono
    {
      get { return EditorPrefs.GetString("Rider_TargetFrameworkVersionOldMono", TargetFrameworkVersionOldMonoDefault); }
      private set { InvokeIfValidVersion(value, val => { EditorPrefs.SetString("Rider_TargetFrameworkVersionOldMono", val); }); }
    }

    public static bool OverrideLangVersion
    {
      get { return EditorPrefs.GetBool("Rider_OverrideLangVersion", false); }
      private set { EditorPrefs.SetBool("Rider_OverrideLangVersion", value);; }
    }

    public static string LangVersion
    {
      get { return EditorPrefs.GetString("Rider_LangVersion", "4"); }
      private set { EditorPrefs.SetString("Rider_LangVersion", value); }
    }

    public static bool RiderInitializedOnce
    {
      get { return EditorPrefs.GetBool("RiderInitializedOnce", false); }
      set { EditorPrefs.SetBool("RiderInitializedOnce", value); }
    }

    private static string RiderPathInternal
    {
      get { return EditorPrefs.GetString("Rider_RiderPath", null); }
      set { EditorPrefs.SetString("Rider_RiderPath", value); }
    }

    // The default "Open C# Project" menu item will use the external script editor to load the .sln
    // file, but unless Unity knows the external script editor can properly load solutions, it will
    // also launch MonoDevelop (or the OS registered app for .sln files). This menu item side steps
    // that issue, and opens the solution in Rider without opening MonoDevelop as well.
    // Unity 2017.1 and later recognise Rider as an app that can load solutions, so this menu isn't
    // needed in newer versions.
    [MenuItem("Assets/Open C# Project in Rider", false, 1000)]
    private static void MenuOpenProject()
    {
      // Force the project files to be sync
      UnityUtils.SyncSolution();

      // Load Project
      PluginEntryPoint.CallRider(string.Format("{0}{1}{0}", "\"", PluginEntryPoint.SlnFile));
    }

    [MenuItem("Assets/Open C# Project in Rider", true, 1000)]
    private static bool ValidateMenuOpenProject()
    {
      if (!PluginEntryPoint.Enabled)
        return false;
      var model = PluginEntryPoint.UnityModels.FirstOrDefault(a => !a.Lifetime.IsTerminated);
      if (model == null)
        return true;
      return false;
    }

    /// <summary>
    /// Forces regeneration of .csproj / .sln files.
    /// </summary>
    [MenuItem("Assets/Sync C# Project", false, 1001)]
    private static void MenuSyncProject()
    {
      // Force the project files to be sync
      UnityUtils.SyncSolution();
    }

    [MenuItem("Assets/Sync C# Project", true, 1001)]
    private static bool ValidateMenuSyncProject()
    {
      return PluginEntryPoint.Enabled;
    }

    /// <summary>
    /// Preferences menu layout
    /// </summary>
    /// <remarks>
    /// Contains all 3 toggles: Enable/Disable; Debug On/Off; Writing Launch File On/Off
    /// </remarks>
    [PreferenceItem("Rider")]
    private static void RiderPreferencesItem()
    {
      EditorGUILayout.BeginVertical();
      EditorGUI.BeginChangeCheck();

      var alternatives = RiderPathLocator.GetAllFoundInfos(SystemInfoRiderPlugin.operatingSystemFamily);
      var paths = alternatives.Select(a => a.Path).ToArray();
      if (alternatives.Any())
      {
        var index = Array.IndexOf(paths, RiderPathInternal);
        var alts = alternatives.Select(s => s.Presentation).ToArray();
        RiderPathInternal = paths[EditorGUILayout.Popup("Rider build:", index == -1 ? 0 : index, alts)];
        EditorGUILayout.HelpBox(RiderPathInternal, MessageType.None);

        if (EditorGUILayout.Toggle(new GUIContent("Rider is default editor"), PluginEntryPoint.Enabled))
        {
          EditorPrefsWrapper.ExternalScriptEditor = RiderPathInternal;
          EditorGUILayout.HelpBox("Unckecking will restore default external editor.", MessageType.None);
        }
        else
        {
          EditorPrefsWrapper.ExternalScriptEditor = string.Empty;
          EditorGUILayout.HelpBox("Checking will set Rider as default external editor", MessageType.None);
        }
      }

      GUILayout.BeginVertical();

      if (UnityUtils.ScriptingRuntime > 0)
      {
        OverrideTargetFrameworkVersion = EditorGUILayout.Toggle(new GUIContent("Override TargetFrameworkVersion"), OverrideTargetFrameworkVersion);
        if (OverrideTargetFrameworkVersion)
        {
          var help = @"TargetFramework >= 4.6 is recommended.";
            TargetFrameworkVersion =
              EditorGUILayout.TextField(
                new GUIContent("For Active profile NET 4.6",
                  help), TargetFrameworkVersion);
            EditorGUILayout.HelpBox(help, MessageType.None);
        }
      }
      else
      {
        OverrideTargetFrameworkVersionOldMono = EditorGUILayout.Toggle(new GUIContent("Override TargetFrameworkVersion"), OverrideTargetFrameworkVersionOldMono);
        if (OverrideTargetFrameworkVersionOldMono)
        {
          var helpOldMono = @"TargetFramework = 3.5 is recommended.
 - With 4.5 Rider may show ambiguous references in UniRx.";

          TargetFrameworkVersionOldMono =
            EditorGUILayout.TextField(
              new GUIContent("For Active profile NET 3.5:",
                helpOldMono), TargetFrameworkVersionOldMono);
          EditorGUILayout.HelpBox(helpOldMono, MessageType.None);
        }
      }

      if (SystemInfoRiderPlugin.operatingSystemFamily == OperatingSystemFamilyRider.Windows)
      {
        var detectedDotnetText = string.Empty;
        var installedFrameworks = GetInstalledNetFrameworks();
        if (installedFrameworks.Any())
          detectedDotnetText = installedFrameworks.OrderBy(v => new Version(v)).Aggregate((a, b) => a+"; "+b);
        EditorGUILayout.HelpBox($"Installed dotnet versions: {detectedDotnetText}", MessageType.None);
      }

      GUILayout.EndVertical();

      EditorGUI.EndChangeCheck();

      EditorGUI.BeginChangeCheck();

      OverrideLangVersion = EditorGUILayout.Toggle(new GUIContent("Override LangVersion"), OverrideLangVersion);
      if (OverrideLangVersion)
      {
        var workaroundUrl = "https://gist.github.com/van800/875ce55eaf88d65b105d010d7b38a8d4";
        var workaroundText = "Use this <color=#0000FF>workaround</color> if overriding doesn't work.";
        var helpLangVersion = @"Avoid overriding, unless there is no particular need.";

        LangVersion =
          EditorGUILayout.TextField(
            new GUIContent("LangVersion:",
              helpLangVersion), LangVersion);
        LinkButton(caption: workaroundText, url: workaroundUrl);
        EditorGUILayout.HelpBox(helpLangVersion, MessageType.None);
      }


      var loggingMsg =
        @"Sets the amount of Rider Debug output. If you are about to report an issue, please select Verbose logging level and attach Unity console output to the issue.";
      SelectedLoggingLevel =
        (LoggingLevel) EditorGUILayout.EnumPopup(new GUIContent("Logging Level", loggingMsg),
          SelectedLoggingLevel);
      EditorGUILayout.HelpBox(loggingMsg, MessageType.None);

      
      EditorGUI.EndChangeCheck();

      if (UnityUtils.UnityVersion < new Version(2018, 2))
      {
        EditorGUI.BeginChangeCheck();
        AssemblyReloadSettings= (AssemblyReloadSettings) EditorGUILayout.EnumPopup("Script Changes While Playing", AssemblyReloadSettings);

        if (EditorGUI.EndChangeCheck())
        {
          if (AssemblyReloadSettings == AssemblyReloadSettings.RecompileAfterFinishedPlaying && EditorApplication.isPlaying)
          {
            EditorApplication.LockReloadAssemblies();
          }
          else
          {
            EditorApplication.UnlockReloadAssemblies();
          }
        }  
      }
      
      var githubRepo = "https://github.com/JetBrains/resharper-unity";
      var caption = $"<color=#0000FF>{githubRepo}</color>";
      LinkButton(caption: caption, url: githubRepo);

      // left for testing purposes
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

      var bClicked = GUILayout.Button(caption, style);

      var rect = GUILayoutUtility.GetLastRect();
      rect.width = style.CalcSize(new GUIContent(caption)).x;
      EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

      if (bClicked)
        Application.OpenURL(url);
    }

    public OperatingSystemFamilyRider OperatingSystemFamilyRider => SystemInfoRiderPlugin.operatingSystemFamily;

    string IPluginSettings.RiderPath
    {
      get { return RiderPathInternal; }
      set { RiderPathInternal = value; }
    }

    internal static class SystemInfoRiderPlugin
    {
      // This call on Linux is extremely slow, so cache it
      private static readonly string ourOperatingSystem = SystemInfo.operatingSystem;

      // Do not rename. Expicitly disabled for consistency/compatibility with future Unity API
      // ReSharper disable once InconsistentNaming
      public static OperatingSystemFamilyRider operatingSystemFamily
      {
        get
        {
          if (ourOperatingSystem.StartsWith("Mac", StringComparison.InvariantCultureIgnoreCase))
          {
            return OperatingSystemFamilyRider.MacOSX;
          }

          if (ourOperatingSystem.StartsWith("Win", StringComparison.InvariantCultureIgnoreCase))
          {
            return OperatingSystemFamilyRider.Windows;
          }

          if (ourOperatingSystem.StartsWith("Lin", StringComparison.InvariantCultureIgnoreCase))
          {
            return OperatingSystemFamilyRider.Linux;
          }

          return OperatingSystemFamilyRider.Other;
        }
      }
    }

  }
}