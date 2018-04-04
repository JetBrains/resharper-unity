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
    private const string FullPluginName = "JetBrains.Rider.Unity.Editor.Plugin.Full.Repacked.dll";
    private static readonly string ourTarget = Path.Combine(AssemblyDirectory, FullPluginName);
    
    public static void InstallRemoveAdditionalPlugins()
    {
      if (!PluginEntryPoint.IsLoadedFromAssets())
      {
        ourLogger.Verbose($"Plugin was not loaded from Assets.");
        return;
      }

      ourLogger.Verbose($"UnityUtils.UnityVersion: {UnityUtils.UnityVersion}");
      if (UnityUtils.UnityVersion >= new Version(5, 6))
      {
        string relPath = @"../../plugins/rider-unity/EditorPlugin";
        if (PluginSettings.SystemInfoRiderPlugin.operatingSystemFamily == OperatingSystemFamilyRider.MacOSX)
          relPath = @"Contents/plugins/rider-unity/EditorPlugin";

        var riderPath = EditorPrefsWrapper.ExternalScriptEditor;
        var origin = new FileInfo(Path.Combine(Path.Combine(riderPath, relPath), FullPluginName));
        if (!origin.Exists)
        {
          ourLogger.Verbose($"${origin} doesn't exist.");
          if (File.Exists(ourTarget))
          {
            ourLogger.Verbose($"Removing ${ourTarget}.");
            File.Delete(ourTarget);
          }
          return;
        }

        if (!File.Exists(ourTarget) ||
            FileVersionInfo.GetVersionInfo(ourTarget) != FileVersionInfo.GetVersionInfo(origin.FullName))
        {
          ourLogger.Verbose($"Coping ${origin} -> ${ourTarget}.");
          origin.CopyTo(ourTarget, true);
        }
      }
      else
      {
        if (File.Exists(ourTarget))
        {
          ourLogger.Verbose($"Removing ${ourTarget}.");
          File.Delete(ourTarget);
        }
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