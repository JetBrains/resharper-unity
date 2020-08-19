using System;
using System.Linq;
using System.Reflection;
using JetBrains.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor
{
  public static class UnityUtils
  {
    private static readonly ILog ourLogger = Log.GetLog("UnityUtils");
    internal static readonly string UnityApplicationVersion = Application.unityVersion;

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
  }
}