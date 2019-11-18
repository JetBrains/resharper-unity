using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using JetBrains.Diagnostics;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor
{
  public static class AdditionalPluginsInstaller
  {
    private static readonly ILog ourLogger = Log.GetLog("AdditionalPluginsInstaller");
    private static readonly string ourTarget = ExecutingAssemblyPath;
    
    public static void UpdateSelf(string fullPluginPath)
    {
      if (string.IsNullOrEmpty(fullPluginPath))
        return;
      
      if (!PluginEntryPoint.IsLoadedFromAssets())
      {
        ourLogger.Verbose($"AdditionalPluginsInstaller disabled. Plugin was not loaded from Assets.");
        return;
      }

      ourLogger.Verbose($"UnityUtils.UnityVersion: {UnityUtils.UnityVersion}");
      if (UnityUtils.UnityVersion >= new Version(5, 6))
      {
        var fullPluginFileInfo = new FileInfo(fullPluginPath);
        if (!fullPluginFileInfo.Exists)
        {
          ourLogger.Verbose($"Plugin {fullPluginPath} doesn't exist.");
          return;
        }

        ourLogger.Verbose($"ourTarget: {ourTarget}");
        if (File.Exists(ourTarget))
        {
          var targetVersionInfo = FileVersionInfo.GetVersionInfo(ourTarget);
          var originVersionInfo = FileVersionInfo.GetVersionInfo(fullPluginFileInfo.FullName);
          ourLogger.Verbose($"{targetVersionInfo.FileVersion} {originVersionInfo.FileVersion} {targetVersionInfo.InternalName} {originVersionInfo.InternalName}");
          if (targetVersionInfo.FileVersion != originVersionInfo.FileVersion ||
            targetVersionInfo.InternalName != originVersionInfo.InternalName)
          {
            ourLogger.Verbose($"Coping ${fullPluginFileInfo} -> ${ourTarget}.");
            File.Delete(ourTarget);
            File.Delete(ourTarget+".meta");
            fullPluginFileInfo.CopyTo(ourTarget, true);
            AssetDatabase.Refresh();
            return;
          }
        }
        
        ourLogger.Verbose($"Plugin {ourTarget} was not updated by {fullPluginPath}.");
      }
    }

    private static string ExecutingAssemblyPath
    {
      get
      {
        var codeBase = Assembly.GetExecutingAssembly().CodeBase;
        var uri = new UriBuilder(codeBase);
        var path = Uri.UnescapeDataString(uri.Path);
        return path;
      }
    }
  }
}