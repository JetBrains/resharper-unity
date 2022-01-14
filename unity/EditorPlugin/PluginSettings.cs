using System;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Diagnostics;
using JetBrains.Rider.Model.Unity;
using JetBrains.Rider.Unity.Editor.Logger;
using UnityEditor;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor
{
  public interface IPluginSettings
  {
    OperatingSystemFamilyRider OperatingSystemFamilyRider { get; }
  }

  public partial class PluginSettings : IPluginSettings
  {
    private static readonly ILog ourLogger = Log.GetLog<PluginSettings>();

    public static LoggingLevel SelectedLoggingLevel
    {
      get => (LoggingLevel) EditorPrefs.GetInt("Rider_SelectedLoggingLevel", 0);
      set
      {
        EditorPrefs.SetInt("Rider_SelectedLoggingLevel", (int) value);
        LogInitializer.InitLog(value);
      }
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
      private set { EditorPrefs.SetBool("Rider_OverrideTargetFrameworkVersion", value); }
    }

    // Only used for Unity 2018.1 and below
    public static ScriptCompilationDuringPlay AssemblyReloadSettings
    {
      get
      {
          if (UnityUtils.UnityVersion >= new Version(2018, 2))
          {
              Debug.Log("Incorrectly accessing old script compilation settings on newer Unity. Use EditorPrefsWrapper.ScriptChangedDuringPlayOptions");
              return ScriptCompilationDuringPlay.RecompileAndContinuePlaying;
          }

          return UnityUtils.ToScriptCompilationDuringPlay(EditorPrefs.GetInt("Rider_AssemblyReloadSettings",
              UnityUtils.FromScriptCompilationDuringPlay(ScriptCompilationDuringPlay.RecompileAndContinuePlaying)));
      }
      set { EditorPrefs.SetInt("Rider_AssemblyReloadSettings", UnityUtils.FromScriptCompilationDuringPlay(value)); }
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
      private set { EditorPrefs.SetBool("Rider_OverrideTargetFrameworkVersionOldMono", value); }
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
      private set { EditorPrefs.SetBool("Rider_OverrideLangVersion", value); }
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