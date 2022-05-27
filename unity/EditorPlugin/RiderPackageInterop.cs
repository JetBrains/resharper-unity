using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using JetBrains.Diagnostics;

namespace JetBrains.Rider.Unity.Editor
{
  public static class RiderPackageInterop
  {
    private static readonly ILog ourLogger = Log.GetLog("RiderPackageInterop");
    private static MethodInfo ourSyncIfNeededMethod;

    public static Assembly GetAssembly()
    {
      var assemblies = AppDomain.CurrentDomain.GetAssemblies();
      var riderPackageAssembly = assemblies
        .FirstOrDefault(assembly => assembly.GetName().Name.Equals("Unity.Rider.Editor"));
      if (riderPackageAssembly == null)
      {
        ourLogger.Verbose("Could not find Unity.Rider.Editor assembly in current AppDomain");
      }

      return riderPackageAssembly;
    }
    
    /// <summary>
    /// This calls SyncIfNeeded in Rider package
    /// </summary>
    public static void SyncSolution()
    {
      if (!TrySyncIfNeeded(true))
        UnityUtils.SyncSolution();
    }

    private static bool IsUnityCompatibleWithRiderPackage()
    {
#if UNITY_2019_2_OR_NEWER
      return true;
#else
      return false;
#endif
    }

    private static bool TrySyncIfNeeded(bool checkProjectFiles)
    {
      if (!IsUnityCompatibleWithRiderPackage())
        return false;
      
      try
      {
        var method = GetOrCreateSyncIfNeededMethod();
        if (method == null)
          return false;
        
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        method.Invoke(null, new object[] { checkProjectFiles });
        stopWatch.Stop();
        var ts = stopWatch.Elapsed;
        
        if (ts > TimeSpan.FromSeconds(10))
          ourLogger.Error($"SyncIfNeeded call took {ts.TotalSeconds} seconds.");
        else
          ourLogger.Verbose($"SyncIfNeeded call took {ts.TotalSeconds} seconds.");
      }
      catch (Exception e)
      {
        ourLogger.Error(e);
        return false;
      }

      return true;
    }

    private static MethodInfo GetOrCreateSyncIfNeededMethod()
    {
      if (ourSyncIfNeededMethod != null)
        return ourSyncIfNeededMethod;
      
      var riderPackageAssembly = GetAssembly();
      if (riderPackageAssembly == null)
      {
        ourLogger.Error("EditorPlugin assembly is null.");
        return null;
      }
      var riderScriptEditorType = riderPackageAssembly.GetType("Packages.Rider.Editor.RiderScriptEditor");
      if (riderScriptEditorType == null)
      {
        ourLogger.Warn("riderScriptEditorType is null.");  
        return null;
      }
        
      ourSyncIfNeededMethod = riderScriptEditorType.GetMethod("SyncIfNeeded", BindingFlags.Static | BindingFlags.Public);
      if (ourSyncIfNeededMethod == null) 
        ourLogger.Info("syncIfNeededMethod is null.");
      return ourSyncIfNeededMethod;
    }
  }
}