using System;
using System.Linq;
using System.Reflection;
using JetBrains.Diagnostics;
using JetBrains.Rider.Model.Unity;
using UnityEditor;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor
{
  public static class UnityUtils
  {
    private static readonly ILog ourLogger = Log.GetLog("UnityUtils");
    internal static string UnityApplicationVersion => Application.unityVersion;

    /// <summary>
    /// Force Unity To Write Project File
    /// </summary>
    public static void SyncSolution()
    {
      var T = Type.GetType("UnityEditor.SyncVS,UnityEditor");
      var syncSolution = T.GetMethod("SyncSolution", BindingFlags.Public | BindingFlags.Static);
      syncSolution.Invoke(null, null);
    }

    public static Version UnityVersion
    {
      get
      {
        var ver = UnityApplicationVersion.Split(".".ToCharArray()).Take(2).Aggregate((a, b) => a + "." + b);
        return new Version(ver);
      }
    }

    public static bool IsInBatchModeAndNotInRiderTests =>
        UnityEditorInternal.InternalEditorUtility.inBatchMode && !IsInRiderTests;

    public static bool IsInRiderTests =>
        Environment.GetCommandLineArgs().Contains("-riderIntegrationTests");

    public static bool UseRiderTestPath =>
        Environment.GetCommandLineArgs().Contains("-riderTestPath");

    public static string UnityEditorLogPath
    {
        get
        {
            var args = Environment.GetCommandLineArgs();
            var commandlineParser = new CommandLineParser(args);
            if (commandlineParser.Options.ContainsKey("-logfile"))
            {
                return commandlineParser.Options["-logfile"];
            }

            return string.Empty;
        }
    }

    private static int ourScriptingRuntimeCached = -1;

    internal static int ScriptingRuntime
    {
      get
      {
        if (ourScriptingRuntimeCached >= 0)
          return ourScriptingRuntimeCached;

        ourScriptingRuntimeCached = 0; // legacy runtime
        try
        {
          // not available in earlier runtime versions
          var property = typeof(EditorApplication).GetProperty("scriptingRuntimeVersion");
          ourScriptingRuntimeCached = (int) property.GetValue(null, null);
          if (ourScriptingRuntimeCached > 0)
            ourLogger.Verbose("Latest runtime detected.");
        }
        catch (Exception)
        {
        }

        return ourScriptingRuntimeCached;
      }
    }

    internal static ScriptCompilationDuringPlay SafeScriptCompilationDuringPlay =>
        UnityVersion >= new Version(2018, 2)
            ? EditorPrefsWrapper.ScriptCompilationDuringPlay
            : PluginSettings.AssemblyReloadSettings;

    internal static ScriptCompilationDuringPlay ToScriptCompilationDuringPlay(int value)
    {
      switch (value)
      {
        case 0: return ScriptCompilationDuringPlay.RecompileAndContinuePlaying;
        case 1: return ScriptCompilationDuringPlay.RecompileAfterFinishedPlaying;
        case 2: return ScriptCompilationDuringPlay.StopPlayingAndRecompile;
        default:
          Debug.Log($"Unexpected value for ScriptCompilationDuringPlay: {value}");
          return ScriptCompilationDuringPlay.RecompileAfterFinishedPlaying;
      }
    }

    internal static int FromScriptCompilationDuringPlay(ScriptCompilationDuringPlay value)
    {
      switch (value)
      {
        case ScriptCompilationDuringPlay.RecompileAndContinuePlaying: return 0;
        case ScriptCompilationDuringPlay.RecompileAfterFinishedPlaying: return 1;
        case ScriptCompilationDuringPlay.StopPlayingAndRecompile: return 2;
        default:
          throw new ArgumentOutOfRangeException(nameof(value), value, null);
      }
    }
  }
}