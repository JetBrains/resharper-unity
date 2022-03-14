using System;
using System.Linq;
using System.Reflection;
using JetBrains.Diagnostics;

namespace JetBrains.Rider.Unity.Editor
{
  public static class RiderPackageInterop
  {
    private static readonly ILog ourLogger = Log.GetLog("RiderPackageInterop");
    
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
    
    public static bool IsUnityCompatibleWithRiderPackage()
    {
#if UNITY_2019_2
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
        var riderPackageAssembly = GetAssembly();
        if (riderPackageAssembly == null)
        {
          ourLogger.Error("EditorPlugin assembly is null.");
          return false;
        }
        var riderScriptEditorType = riderPackageAssembly.GetType("Packages.Rider.Editor.RiderScriptEditor");
        if (riderScriptEditorType == null)
        {
          ourLogger.Warn("riderScriptEditorType is null.");  
          return false;
        }
        
        var syncIfNeededMethod = riderScriptEditorType.GetMethod("SyncIfNeeded", BindingFlags.Static | BindingFlags.Public); // Rider package prior to 3.0.13 doesn't have it
        if (syncIfNeededMethod == null)
        {
          ourLogger.Info("syncIfNeededMethod is null.");
          return false;
        }

        syncIfNeededMethod.Invoke(null, new object[] { checkProjectFiles });
      }
      catch (Exception e)
      {
        ourLogger.Error(e);
        return false;
      }

      return true;
    }
  }
}