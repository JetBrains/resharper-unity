using System;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Diagnostics;
using JetBrains.Diagnostics.Internal;
using JetBrains.Lifetimes;
using UnityEditor;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor
{
  public interface IPluginSettings
  {
    OperatingSystemFamilyRider OperatingSystemFamilyRider { get; }
  }

  public enum AssemblyReloadSettings
  {
    RecompileAndContinuePlaying = 0,
    RecompileAfterFinishedPlaying = 1,
    StopPlayingAndRecompile = 2
  }

  public class PluginSettings : IPluginSettings
  {
    private static readonly ILog ourLogger = Log.GetLog<PluginSettings>();
    
    public static LoggingLevel SelectedLoggingLevel
    {
      get => (LoggingLevel) EditorPrefs.GetInt("Rider_SelectedLoggingLevel", 0);
      set
      {
        EditorPrefs.SetInt("Rider_SelectedLoggingLevel", (int) value);
        InitLog();
      }
    }

    public static void InitLog()
    {
      if (SelectedLoggingLevel > LoggingLevel.OFF) 
        Log.DefaultFactory = Log.CreateFileLogFactory(Lifetime.Eternal, PluginEntryPoint.LogPath, true, SelectedLoggingLevel);
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
      var referenceAssembliesPaths = new[]
      {
        Path.Combine(programFiles86, @"Reference Assemblies\Microsoft\Framework\.NETFramework"),
        Path.Combine(programFiles86, @"Reference Assemblies\Microsoft\Framework") //RIDER-42873
      }.Select(s => new DirectoryInfo(s)).Where(a=>a.Exists).ToArray();
      
      if (!referenceAssembliesPaths.Any())
        return new string[0];

      var availableVersions = referenceAssembliesPaths
        .SelectMany(a=>a.GetDirectories("v*"))
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
      set { EditorPrefs.SetInt("Rider_AssemblyReloadSettings", (int) value);; }
    }
    
    public static bool UseLatestRiderFromToolbox
    {
      get { return EditorPrefs.GetBool("UseLatestRiderFromToolbox", true); }
      set { EditorPrefs.SetBool("UseLatestRiderFromToolbox", value); }
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

    public static bool LogEventsCollectorEnabled
    {
      get { return EditorPrefs.GetBool("Rider_LogEventsCollectorEnabled", true); }
      private set { EditorPrefs.SetBool("Rider_LogEventsCollectorEnabled", value); }
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
      EditorGUIUtility.labelWidth = 200f;
      EditorGUILayout.BeginVertical();

      var alternatives = RiderPathLocator.GetAllFoundInfos(SystemInfoRiderPlugin.operatingSystemFamily);
      if (alternatives.Any()) // from known locations
      {
        var paths = alternatives.Select(a => a.Path).ToList();
        var externalEditor = EditorPrefsWrapper.ExternalScriptEditor;
        var alts = alternatives.Select(s => s.Presentation).ToList();

        if (!paths.Contains(externalEditor))
        {
          paths.Add(externalEditor);
          alts.Add(externalEditor);
        }

        var index = paths.IndexOf(externalEditor);
        
        
        var result = paths[EditorGUILayout.Popup("Rider build:", index == -1 ? 0 : index, alts.ToArray())];
        
        EditorPrefsWrapper.ExternalScriptEditor = result;
      }
      
      if (PluginEntryPoint.IsRiderDefaultEditor() && !RiderPathProvider.RiderPathExist(EditorPrefsWrapper.ExternalScriptEditor, SystemInfoRiderPlugin.operatingSystemFamily))
      {
        EditorGUILayout.HelpBox($"Rider is selected as preferred ExternalEditor, but doesn't exist on disk {EditorPrefsWrapper.ExternalScriptEditor}", MessageType.Warning);
      }

      UseLatestRiderFromToolbox = EditorGUILayout.Toggle(new GUIContent("Update Rider to latest version"),  UseLatestRiderFromToolbox);
      
      GUILayout.BeginVertical();
      LogEventsCollectorEnabled = EditorGUILayout.Toggle(new GUIContent("Pass Console to Rider:"), LogEventsCollectorEnabled);

      if (UnityUtils.ScriptingRuntime > 0)
      {
        OverrideTargetFrameworkVersion = EditorGUILayout.Toggle(new GUIContent("Override TargetFrameworkVersion:"), OverrideTargetFrameworkVersion);
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
        OverrideTargetFrameworkVersionOldMono = EditorGUILayout.Toggle(new GUIContent("Override TargetFrameworkVersion:"), OverrideTargetFrameworkVersionOldMono);
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

      // Unity 2018.1 doesn't require installed dotnet framework, it references everything from Unity installation
      if (SystemInfoRiderPlugin.operatingSystemFamily == OperatingSystemFamilyRider.Windows && UnityUtils.UnityVersion < new Version(2018, 1))
      {
        var detectedDotnetText = string.Empty;
        var installedFrameworks = GetInstalledNetFrameworks();
        if (installedFrameworks.Any())
          detectedDotnetText = installedFrameworks.OrderBy(v => new Version(v)).Aggregate((a, b) => a+"; "+b);
        EditorGUILayout.HelpBox($"Installed dotnet versions: {detectedDotnetText}", MessageType.None);
      }

      GUILayout.EndVertical();

      OverrideLangVersion = EditorGUILayout.Toggle(new GUIContent("Override LangVersion:"), OverrideLangVersion);
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
      GUILayout.Label("");
      
      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.PrefixLabel("Log file:");
      var previous = GUI.enabled;
      GUI.enabled = previous && SelectedLoggingLevel != LoggingLevel.OFF;
      var button = GUILayout.Button(new GUIContent("Open log"));
      if (button)
      {
        //UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(PluginEntryPoint.LogPath, 0);
        // works much faster than the commented code, when Rider is already started
        PluginEntryPoint.OpenAssetHandler.OnOpenedAsset(PluginEntryPoint.LogPath, 0, 0);
      }
      GUI.enabled = previous;
      GUILayout.EndHorizontal();
      
      var loggingMsg =
        @"Sets the amount of Rider Debug output. If you are about to report an issue, please select Verbose logging level and attach Unity console output to the issue.";
      SelectedLoggingLevel =
        (LoggingLevel) EditorGUILayout.EnumPopup(new GUIContent("Logging Level:", loggingMsg),
          SelectedLoggingLevel);

      
      EditorGUILayout.HelpBox(loggingMsg, MessageType.None);
      

      if (UnityUtils.UnityVersion < new Version(2018, 2))
      {
        EditorGUI.BeginChangeCheck();
        AssemblyReloadSettings = (AssemblyReloadSettings) EditorGUILayout.EnumPopup("Script Changes during Playing:", AssemblyReloadSettings);

        if (EditorGUI.EndChangeCheck())
        {
          if (AssemblyReloadSettings == AssemblyReloadSettings.RecompileAfterFinishedPlaying && EditorApplication.isPlaying)
          {
            ourLogger.Info("LockReloadAssemblies");
            EditorApplication.LockReloadAssemblies();
          }
          else
          {
            ourLogger.Info("UnlockReloadAssemblies");
            EditorApplication.UnlockReloadAssemblies();
          }
        }  
      }
      
      var githubRepo = "https://github.com/JetBrains/resharper-unity";
      var caption = $"<color=#0000FF>{githubRepo}</color>";
      LinkButton(caption: caption, url: githubRepo);
      
      GUILayout.FlexibleSpace();
      GUILayout.BeginHorizontal();
      
      GUILayout.FlexibleSpace();
      var version = Assembly.GetExecutingAssembly().GetName().Version;
      GUILayout.Label("Plugin version: " + version, new GUIStyle()
      {
        normal = new GUIStyleState()
        {
          textColor = new Color(0, 0, 0, .6f),
        }, 
        margin = new RectOffset(4, 4, 4, 4),
      });
      
      GUILayout.EndHorizontal();
      
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

    internal static class SystemInfoRiderPlugin
    {
      // This call on Linux is extremely slow, so cache it
      private static readonly string ourOperatingSystem = SystemInfo.operatingSystem;

      // Do not rename. Explicitly disabled for consistency/compatibility with future Unity API
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