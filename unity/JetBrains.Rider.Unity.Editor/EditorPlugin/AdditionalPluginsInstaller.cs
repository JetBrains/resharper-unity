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
    private static string pluginName = "JetBrains.Rider.Unity.Editor.Plugin.Repacked.dll";
    private static string ge56PluginName = "JetBrains.Rider.Unity.Editor.Plugin.Ge56.dll";
    private static string target = Path.Combine(AssemblyDirectory, ge56PluginName);
    
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
        var origin = new FileInfo(Path.Combine(Path.Combine(riderPath, relPath), ge56PluginName));
        if (!origin.Exists)
        {
          ourLogger.Verbose($"${origin} doesn't exist.");
          if (File.Exists(target))
          {
            ourLogger.Verbose($"Removing ${target}.");
            File.Delete(target);
          }
          return;
        }

        if (!File.Exists(target) ||
            FileVersionInfo.GetVersionInfo(target) != FileVersionInfo.GetVersionInfo(origin.FullName))
        {
          ourLogger.Verbose($"Coping ${origin} -> ${target}.");
          origin.CopyTo(target, true);
        }
      }
      else
      {
        if (File.Exists(target))
        {
          ourLogger.Verbose($"Removing ${target}.");
          File.Delete(target);
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