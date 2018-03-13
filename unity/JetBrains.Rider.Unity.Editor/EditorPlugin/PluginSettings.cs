using System;
using System.IO;
using System.Linq;
using JetBrains.Util;
using UnityEditor;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor
{
  public interface IPluginSettings
  {
    OperatingSystemFamilyRider OperatingSystemFamilyRider { get; }
    string RiderPath { get; set; }
  } 
  
  public class PluginSettings : IPluginSettings
  {   
    private static LoggingLevel ourSelectedLoggingLevel = (LoggingLevel) EditorPrefs.GetInt("Rider_SelectedLoggingLevel", 4);
    
    internal static LoggingLevel SelectedLoggingLevel
    {
      get => ourSelectedLoggingLevel;
      private set
      {
        EditorPrefs.SetInt("Rider_SelectedLoggingLevel", (int) value);
        ourSelectedLoggingLevel = value;
      }
    }

    public static string[] GetInstalledNetFrameworks()
    {
      if (SystemInfoRiderPlugin.operatingSystemFamily != OperatingSystemFamilyRider.Windows)
        throw new InvalidOperationException("GetTargetFrameworkVersionWindowsMono2 is designed for Windows only");
      
      var dir = new DirectoryInfo(@"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework");
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

    private static bool InvokeIfValidVersion(string value, Action<string> action)
    {
      try
      {
        // ReSharper disable once ObjectCreationAsStatement
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
    
    public static bool OverrideTargetFrameworkVersion
    {
      get { return EditorPrefs.GetBool("Rider_OverrideTargetFrameworkVersion", false); }
      private set { EditorPrefs.SetBool("Rider_OverrideTargetFrameworkVersion", value);; }
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
      return PluginEntryPoint.Enabled && !PluginEntryPoint.CheckConnectedToBackendSync();
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

      var alternatives = RiderPathLocator.GetAllFoundPaths(SystemInfoRiderPlugin.operatingSystemFamily);
      if (alternatives.Any())
      {
        var index = Array.IndexOf(alternatives, RiderPathInternal);
        var alts = alternatives.Select(s => s.Replace("/", ":"))
          .ToArray(); // hack around https://fogbugz.unity3d.com/default.asp?940857_tirhinhe3144t4vn
        RiderPathInternal = alternatives[EditorGUILayout.Popup("Rider executable:", index == -1 ? 0 : index, alts)];
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
        var detectedDotnetText = GetInstalledNetFrameworks().OrderBy(v => new Version(v)).Aggregate((a, b) => a+"; "+b);
        EditorGUILayout.HelpBox($"Installed dotnet versions: {detectedDotnetText}", MessageType.None);
      }

      GUILayout.EndVertical();

      EditorGUI.EndChangeCheck();

      EditorGUI.BeginChangeCheck();

      var loggingMsg =
        @"Sets the amount of Rider Debug output. If you are about to report an issue, please select Verbose logging level and attach Unity console output to the issue.";
      SelectedLoggingLevel =
        (LoggingLevel) EditorGUILayout.EnumPopup(new GUIContent("Logging Level", loggingMsg),
          SelectedLoggingLevel);
      EditorGUILayout.HelpBox(loggingMsg, MessageType.None);

      EditorGUI.EndChangeCheck();

      var githubRepo = "https://github.com/JetBrains/resharper-unity";
      LinkButton(caption: githubRepo, url: githubRepo);

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
      caption = $"<color=#0000FF>{caption}</color>";

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
      // Do not rename. Expicitly disabled for consistency/compatibility with future Unity API
      // ReSharper disable once InconsistentNaming
      public static OperatingSystemFamilyRider operatingSystemFamily
      {
        get
        {
          if (SystemInfo.operatingSystem.StartsWith("Mac", StringComparison.InvariantCultureIgnoreCase))
          {
            return OperatingSystemFamilyRider.MacOSX;
          }

          if (SystemInfo.operatingSystem.StartsWith("Win", StringComparison.InvariantCultureIgnoreCase))
          {
            return OperatingSystemFamilyRider.Windows;
          }

          if (SystemInfo.operatingSystem.StartsWith("Lin", StringComparison.InvariantCultureIgnoreCase))
          {
            return OperatingSystemFamilyRider.Linux;
          }

          return OperatingSystemFamilyRider.Other;
        }
      }
    }

  }
}