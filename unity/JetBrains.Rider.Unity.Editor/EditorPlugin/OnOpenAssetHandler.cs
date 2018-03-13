using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.Platform.Unity.Model;
using JetBrains.Rider.Unity.Editor.AssetPostprocessors;
using JetBrains.Rider.Unity.Editor.NonUnity;
using JetBrains.Util.Logging;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor
{
  internal class OnOpenAssetHandler
  {
    private readonly ILog myLogger = Log.GetLog<OnOpenAssetHandler>();
    private readonly RProperty<UnityModel> myModel;
    private readonly RiderPathLocator myRiderPathLocator;
    private readonly IPluginSettings myPluginSettings;
    private readonly string mySlnFile;

    public OnOpenAssetHandler(RProperty<UnityModel> model, RiderPathLocator riderPathLocator, IPluginSettings pluginSettings, string slnFile)
    {
      myModel = model;
      myRiderPathLocator = riderPathLocator;
      myPluginSettings = pluginSettings;
      mySlnFile = slnFile;
    }

    public bool OnOpenedAsset(int instanceID, int line)
    {
      // determine asset that has been double clicked in the project view
      var selected = EditorUtility.InstanceIDToObject(instanceID);

      var assetFilePath = Path.GetFullPath(AssetDatabase.GetAssetPath(selected));
      if (!(selected.GetType().ToString() == "UnityEditor.MonoScript" ||
            selected.GetType().ToString() == "UnityEngine.Shader" ||
            (selected.GetType().ToString() == "UnityEngine.TextAsset" &&
//#if UNITY_5 || UNITY_5_5_OR_NEWER
//          EditorSettings.projectGenerationUserExtensions.Contains(Path.GetExtension(assetFilePath).Substring(1))
//#else
             EditorSettings.externalVersionControl.Contains(Path.GetExtension(assetFilePath).Substring(1))
//#endif
            )))
        return false;

      var modifiedSource = EditorPrefs.GetBool(ModificationPostProcessor.ModifiedSource, false);
      myLogger.Verbose("ModifiedSource: {0} EditorApplication.isPlaying: {1} EditorPrefsWrapper.AutoRefresh: {2}", modifiedSource, EditorApplication.isPlaying, EditorPrefsWrapper.AutoRefresh);

      if (modifiedSource && !EditorApplication.isPlaying && EditorPrefsWrapper.AutoRefresh)
      {
        UnityUtils.SyncSolution(); // added to handle opening file, which was just recently created.
        EditorPrefs.SetBool(ModificationPostProcessor.ModifiedSource, false);
      }

      var model = myModel.Maybe.ValueOrDefault;
      if (model != null)
      {
        if (PluginEntryPoint.CheckConnectedToBackendSync())
        {
          const int column = 0;
          myLogger.Verbose("Calling OpenFileLineCol: {0}, {1}, {2}", assetFilePath, line, column);
          model.OpenFileLineCol.Start(new RdOpenFileArgs(assetFilePath, line, column));
          if (model.RiderProcessId.HasValue())
            ActivateWindow(model.RiderProcessId.Value);
          else
            ActivateWindow();
          // todo: maybe fallback to CallRider, if returns false
          return true;
        }
      }

      var args = string.Format("{0}{1}{0} --line {2} {0}{3}{0}", "\"", mySlnFile, line, assetFilePath);
      return CallRider(args);
    }

    public bool CallRider(string args)
    {
      var defaultApp = myRiderPathLocator.GetDefaultRiderApp(EditorPrefsWrapper.ExternalScriptEditor, RiderPathLocator.GetAllFoundPaths(myPluginSettings.OperatingSystemFamilyRider));
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

      ActivateWindow();
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