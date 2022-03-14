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
      TrySetCheckProjectFiles(true);
      try
      {
        UnityUtils.SyncSolution();
      }
      finally
      {
        TrySetCheckProjectFiles(false);
      }
    }
    
    public static bool IsUnityCompatibleWithRiderPackage()
    {
#if UNITY_2019_2
      return true;
#else
      return false;
#endif
    }

    private static void TrySetCheckProjectFiles(bool checkProjectFiles)
    {
      if (!IsUnityCompatibleWithRiderPackage())
        return;
      
      try
      {
        var riderPackageAssembly = GetAssembly();
        if (riderPackageAssembly == null)
        {
          ourLogger.Error("EditorPlugin assembly is null.");
          return;
        }
        var editorPluginCookieType = riderPackageAssembly.GetType("Packages.Rider.Editor.ProjectGeneration.EditorPluginCookie");
        if (editorPluginCookieType == null)
        {
          ourLogger.Warn("editorPluginCookieType is null."); // Rider package prior to 3.0.13 doesn't have it 
          return;
        }
        var baseType = editorPluginCookieType.BaseType;
        if (baseType == null)
        {
          ourLogger.Error("editorPluginCookieType.BaseType is null.");
          return;
        }
        var instance = baseType.GetProperty("instance");
        if (instance == null)
        {
          ourLogger.Error("instance of EditorPluginCookieType is null.");
          return;
        }
        var instanceVal = instance.GetValue(null, new object[] { });
        var checkProjectFilesField = editorPluginCookieType.GetField("checkProjectFiles");
        if (checkProjectFilesField == null)
        {
          ourLogger.Error("checkProjectFilesField is null.");
          return;
        }
        checkProjectFilesField.SetValue(instanceVal, checkProjectFiles);
      }
      catch (Exception e)
      {
        ourLogger.Error(e);
      }
    }
  }
}