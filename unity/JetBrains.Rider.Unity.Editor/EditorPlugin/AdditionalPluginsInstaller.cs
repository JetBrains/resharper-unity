using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using JetBrains.Util.Logging;

namespace JetBrains.Rider.Unity.Editor
{
  public static class AdditionalPluginsInstaller
  {
    private static readonly ILog ourLogger = Log.GetLog("AdditionalPluginsInstaller");
    private const string BasicPluginName = "JetBrains.Rider.Unity.Editor.Plugin.Repacked.dll";
    private static readonly string ourTarget = Path.Combine(AssemblyDirectory, BasicPluginName);
    
    public static void InstallRemoveAdditionalPlugins(string fullPluginPath)
    {
      if (!PluginEntryPoint.IsLoadedFromAssets())
      {
        ourLogger.Verbose($"Plugin was not loaded from Assets.");
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

        if (File.Exists(ourTarget))
        {
          if (FileVersionInfo.GetVersionInfo(ourTarget) != FileVersionInfo.GetVersionInfo(fullPluginFileInfo.FullName) ||
            Assembly.GetExecutingAssembly().GetName().Name == Path.GetFileNameWithoutExtension(BasicPluginName))
          {
            ourLogger.Verbose($"Coping ${fullPluginFileInfo} -> ${ourTarget}.");
            fullPluginFileInfo.CopyTo(ourTarget, true);
            return;
          }
        }
        
        ourLogger.Verbose($"Plugin {ourTarget} was not updated by {fullPluginPath}.");
      }
    }

    private static string AssemblyDirectory
    {
      get
      {
        var codeBase = Assembly.GetExecutingAssembly().CodeBase;
        var uri = new UriBuilder(codeBase);
        var path = Uri.UnescapeDataString(uri.Path);
        return Path.GetDirectoryName(path);
      }
    }
  }
}