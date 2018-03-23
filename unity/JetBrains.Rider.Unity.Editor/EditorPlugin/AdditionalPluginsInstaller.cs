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
    
    public static bool TryInstallAdditionalPlugins()
    {
      if (!PluginEntryPoint.IsLoadedFromAssets())
      {
        ourLogger.Verbose($"Plugin was not loaded from Assets.");
        return false;
      }

      if (UnityUtils.UnityVersion < new Version(5, 6))
      {
        ourLogger.Verbose($"UnityUtils.UnityVersion: {UnityUtils.UnityVersion}");
        return false;
      }
      
      string relPath = @"../../plugins/rider-unity/EditorPlugin";
      if (PluginSettings.SystemInfoRiderPlugin.operatingSystemFamily == OperatingSystemFamilyRider.MacOSX)
        relPath = @"Contents/plugins/rider-unity/EditorPlugin";
      
      var riderPath = EditorPrefsWrapper.ExternalScriptEditor;
      var origin = new FileInfo(Path.Combine(Path.Combine(riderPath, relPath), ge56PluginName));
      if (!origin.Exists)
      {
        ourLogger.Warn($"${origin} doesn't exist.");
        return false;
      }

      var target = Path.Combine(AssemblyDirectory, origin.Name);
      if (!File.Exists(target) || FileVersionInfo.GetVersionInfo(target) != FileVersionInfo.GetVersionInfo(origin.FullName))
      {
        ourLogger.Verbose($"Coping ${origin} -> ${target}.");
        origin.CopyTo(target, true);
      }
      return true;
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