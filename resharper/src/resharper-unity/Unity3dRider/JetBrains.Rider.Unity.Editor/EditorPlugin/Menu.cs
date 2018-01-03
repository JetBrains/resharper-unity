using System;
using System.IO;
using System.Linq;
using JetBrains.Util;
using UnityEditor;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor
{
  public static class Menu
  {
    internal static LoggingLevel SelectedLoggingLevel { get; set; }

    internal static LoggingLevel SelectedLoggingLevelMainThread
    {
      get { return (LoggingLevel) EditorPrefs.GetInt("Rider_SelectedLoggingLevel", 1); }
      private set
      {
        SelectedLoggingLevel = value;
        EditorPrefs.SetInt("Rider_SelectedLoggingLevel", (int) value);
      }
    }
    
    private static string GetTargetFrameworkVersionDefault(string defaultValue)
    {
      if (SystemInfoRiderPlugin.operatingSystemFamily == OperatingSystemFamilyRider.Windows)
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


    private static bool TryCatch(string value, Action<string> action)
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


    public static bool SendConsoleToRider
    {
      get { return EditorPrefs.GetBool("Rider_SendConsoleToRider", false); }
      set { EditorPrefs.SetBool("Rider_SendConsoleToRider", value); }
    }

    public static string TargetFrameworkVersion
    {
      get { return EditorPrefs.GetString("Rider_TargetFrameworkVersion", GetTargetFrameworkVersionDefault("4.6")); }
      set { TryCatch(value, val => { EditorPrefs.SetString("Rider_TargetFrameworkVersion", val); }); }
    }

    public static string TargetFrameworkVersionOldMono
    {
      get
      {
        return EditorPrefs.GetString("Rider_TargetFrameworkVersionOldMono", GetTargetFrameworkVersionDefault("3.5"));
      }
      set { TryCatch(value, val => { EditorPrefs.SetString("Rider_TargetFrameworkVersionOldMono", val); }); }
    }

    public static string RiderPath
    {
      get { return EditorPrefs.GetString("Rider_RiderPath", RiderPlugin.GetAllRiderPaths().FirstOrDefault()); }
      set { EditorPrefs.SetString("Rider_RiderPath", value); }
    }

    public static bool RiderInitializedOnce
    {
      get { return EditorPrefs.GetBool("RiderInitializedOnce", false); }
      set { EditorPrefs.SetBool("RiderInitializedOnce", value); }
    }

    [MenuItem("Assets/Open C# Project in Rider", false, 1000)]
    private static void MenuOpenProject()
    {
      // Force the project files to be sync
      UnityApplication.SyncSolution();

      // Load Project
      RiderPlugin.CallRider(string.Format("{0}{1}{0}", "\"", RiderPlugin.SlnFile));
    }

    [MenuItem("Assets/Open C# Project in Rider", true, 1000)]
    private static bool ValidateMenuOpenProject()
    {
      return RiderPlugin.Enabled;
    }

    /// <summary>
    /// JetBrains Rider Integration Preferences Item
    /// </summary>
    /// <remarks>
    /// Contains all 3 toggles: Enable/Disable; Debug On/Off; Writing Launch File On/Off
    /// </remarks>
    [PreferenceItem("Rider")]
    private static void RiderPreferencesItem()
    {
      EditorGUILayout.BeginVertical();
      EditorGUI.BeginChangeCheck();

      var alternatives = RiderPlugin.GetAllRiderPaths();
      if (alternatives.Any())
      {
        int index = Array.IndexOf(alternatives, RiderPath);
        var alts = alternatives.Select(s => s.Replace("/", ":"))
          .ToArray(); // hack around https://fogbugz.unity3d.com/default.asp?940857_tirhinhe3144t4vn
        RiderPath = alternatives[EditorGUILayout.Popup("Rider executable:", index == -1 ? 0 : index, alts)];
        if (EditorGUILayout.Toggle(new GUIContent("Rider is default editor"), RiderPlugin.Enabled))
        {
          UnityApplication.SetExternalScriptEditor(RiderPath);
          EditorGUILayout.HelpBox("Unckecking will restore default external editor.", MessageType.None);
        }
        else
        {
          UnityApplication.SetExternalScriptEditor(string.Empty);
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
      SelectedLoggingLevelMainThread =
        (LoggingLevel) EditorGUILayout.EnumPopup(new GUIContent("Logging Level", loggingMsg),
          SelectedLoggingLevelMainThread);
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
  }
}