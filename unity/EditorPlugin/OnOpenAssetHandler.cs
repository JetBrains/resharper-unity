﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.Rider.Unity.Editor.AssetPostprocessors;
using JetBrains.Rider.Unity.Editor.NonUnity;
using JetBrains.Util.Logging;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor
{
  internal class OnOpenAssetHandler
  {
    private readonly ILog myLogger = Log.GetLog<OnOpenAssetHandler>();
    private readonly RiderPathLocator myRiderPathLocator;
    private readonly IPluginSettings myPluginSettings;
    private readonly string mySlnFile;

    public OnOpenAssetHandler(RiderPathLocator riderPathLocator, IPluginSettings pluginSettings, string slnFile)
    {
      myRiderPathLocator = riderPathLocator;
      myPluginSettings = pluginSettings;
      mySlnFile = slnFile;
    }
    
    public bool OnOpenedAsset(int instanceID, int line)
    {
      // determine asset that has been double clicked in the project view
      var selected = EditorUtility.InstanceIDToObject(instanceID);
      var assetPath = AssetDatabase.GetAssetPath(selected);

      if (string.IsNullOrEmpty(assetPath)) // RIDER-16784
        return false;

      var assetFilePath = Path.GetFullPath(assetPath);
      if (!(selected.GetType().ToString() == "UnityEditor.MonoScript" ||
            selected.GetType().ToString() == "UnityEngine.Shader" ||
            selected.GetType().ToString() == "UnityEngine.Experimental.UIElements.VisualTreeAsset" ||
            selected.GetType().ToString() == "UnityEngine.StyleSheets.StyleSheet" ||
            GetExtensionStrings().Contains(Path.GetExtension(assetFilePath).Substring(1))
            ))
        return false;

      return OnOpenedAsset(assetFilePath, line);
    }

    private static string[] GetExtensionStrings()
    {
      var extensionStrings = new[] {"ts", "bjs", "javascript", "json", "html", "shader"};
      var propertyInfo = typeof(EditorSettings)
        .GetProperty("projectGenerationUserExtensions", BindingFlags.Public | BindingFlags.Static);
      if (propertyInfo != null)
      {
        var value = propertyInfo.GetValue(null, null);
        extensionStrings = (string[]) value;
      }
      
      // https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/VisualStudioIntegration/SolutionSynchronizer.cs#L50
      var builtinSupportedExtensions = new[] {"template", "compute", "cginc", "hlsl", "glslinc"}; // todo: get it via reflection
      var list = extensionStrings.ToList();
      list.AddRange(builtinSupportedExtensions);
      extensionStrings = list.ToArray();

      return extensionStrings;
    }

    [UsedImplicitly] // https://github.com/JetBrains/resharper-unity/issues/475
    public bool OnOpenedAsset(string assetFilePath, int line)
    {
      var modifiedSource = EditorPrefs.GetBool(ModificationPostProcessor.ModifiedSource, false);
      myLogger.Verbose("ModifiedSource: {0} EditorApplication.isPlaying: {1} EditorPrefsWrapper.AutoRefresh: {2}",
        modifiedSource, EditorApplication.isPlaying, EditorPrefsWrapper.AutoRefresh);

      if (modifiedSource && !EditorApplication.isPlaying && EditorPrefsWrapper.AutoRefresh || !File.Exists(PluginEntryPoint.SlnFile))
      {
        UnityUtils.SyncSolution(); // added to handle opening file, which was just recently created.
        EditorPrefs.SetBool(ModificationPostProcessor.ModifiedSource, false);
      }

      var models = PluginEntryPoint.UnityModels.Where(a=>!a.Lifetime.IsTerminated).ToArray();
      if (models.Any())
      {
        var model = models.First().Model;
        if (PluginEntryPoint.CheckConnectedToBackendSync(model))
        {
          const int column = 0;
          myLogger.Verbose("Calling OpenFileLineCol: {0}, {1}, {2}", assetFilePath, line, column);
          if (model.RiderProcessId.HasValue())
            ActivateWindow(model.RiderProcessId.Value);
          else
            ActivateWindow();
          
          model.OpenFileLineCol.Start(new RdOpenFileArgs(assetFilePath, line, column));
          
          // todo: maybe fallback to CallRider, if returns false
          return true;
        }
      }

      var args = string.Format("{0}{1}{0} --line {2} {0}{3}{0}", "\"", mySlnFile, line, assetFilePath);
      return CallRider(args);
    }

    public bool CallRider(string args)
    {
      var paths = RiderPathLocator.GetAllFoundPaths(myPluginSettings.OperatingSystemFamilyRider);
      var defaultApp = myRiderPathLocator.GetDefaultRiderApp(EditorPrefsWrapper.ExternalScriptEditor, paths);
      if (string.IsNullOrEmpty(defaultApp))
      {
        return false;
      }

      var proc = new Process();
      if (myPluginSettings.OperatingSystemFamilyRider == OperatingSystemFamilyRider.MacOSX)
      {
        proc.StartInfo.FileName = "open";
        proc.StartInfo.Arguments = string.Format("-n {0}{1}{0} --args {2}", "\"", "/" + defaultApp, args);
        myLogger.Verbose("{0} {1}", proc.StartInfo.FileName, proc.StartInfo.Arguments);
      }
      else
      {
        proc.StartInfo.FileName = defaultApp;
        proc.StartInfo.Arguments = args;
        myLogger.Verbose("{2}{0}{2}" + " {1}", proc.StartInfo.FileName, proc.StartInfo.Arguments, "\"");
      }

      proc.StartInfo.UseShellExecute = true; // avoid HandleInheritance
      proc.Start();

      ActivateWindow(proc.Id);
      return true;
    }

    private void ActivateWindow(int? processId=null)
    {
      if (myPluginSettings.OperatingSystemFamilyRider != OperatingSystemFamilyRider.Windows)
        return;
      
      try
      {
        var process = processId == null ? GetRiderProcess() : Process.GetProcessById((int)processId);
        if (process == null)
          return;
        
        if (process.Id>0)
          User32Dll.AllowSetForegroundWindow(process.Id);
        
        // Collect top level windows
        var topLevelWindows = User32Dll.GetTopLevelWindowHandles();
        // Get process main window title
        var windowHandle = topLevelWindows.FirstOrDefault(hwnd => User32Dll.GetWindowProcessId(hwnd) == process.Id);
        myLogger.Verbose("ActivateWindow: {0} {1}", process.Id, windowHandle);
        if (windowHandle != IntPtr.Zero)
        {
          //User32Dll.ShowWindow(windowHandle, 9); //SW_RESTORE = 9
          User32Dll.SetForegroundWindow(windowHandle);
        }

      }
      catch (Exception e)
      {
        myLogger.Warn("Exception on ActivateWindow: " + e);
      }
    }

    private static Process GetRiderProcess()
    {
      var process = Process.GetProcesses().FirstOrDefault(p =>
      {
        string processName;
        try
        {
          processName =
            p.ProcessName; // some processes like kaspersky antivirus throw exception on attempt to get ProcessName
        }
        catch (Exception)
        {
          return false;
        }

        return !p.HasExited && processName.ToLower().Contains("rider");
      });
      return process;
    }
  }
}