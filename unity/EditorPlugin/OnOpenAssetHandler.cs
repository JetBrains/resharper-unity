using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using JetBrains.Collections.Viewable;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Rider.Unity.Editor.NonUnity;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Rider.PathLocator;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor
{
  public class OnOpenAssetHandler
  {
    private readonly ILog myLogger = Log.GetLog<OnOpenAssetHandler>();
    private readonly Lifetime myLifetime;
    private readonly RiderPathProvider myRiderPathProvider;
    private readonly IPluginSettings myPluginSettings;
    private readonly string mySlnFile;

    internal OnOpenAssetHandler(Lifetime lifetime,
                                RiderPathProvider riderPathProvider,
                                IPluginSettings pluginSettings,
                                string slnFile)
    {
      myLifetime = lifetime;
      myRiderPathProvider = riderPathProvider;
      myPluginSettings = pluginSettings;
      mySlnFile = slnFile;
    }

    // DO NOT RENAME OR CHANGE SIGNATURE!
    // Used from package via reflection. Must remain public and non-static.
    // Note that the package gets the type from PluginEntryPoint.OpenAssetHandler, so name, namespace and visibility of
    // the class is not important
    [PublicAPI]
    public bool OnOpenedAsset(int instanceID, int line, int column)
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
            Path.HasExtension(assetPath) && GetExtensionStrings().Contains(Path.GetExtension(assetFilePath).Substring(1))
            ))
        return false;

      return OnOpenedAsset(assetFilePath, line, column);
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

    // DO NOT RENAME OR CHANGE SIGNATURE!
    // Created as a public API for external users. See https://github.com/JetBrains/resharper-unity/issues/475
    [PublicAPI]
    public bool OnOpenedAsset(string assetFilePath, int line, int column = 0)
    {
      var modifiedSource = EditorPrefs.GetBool(ModificationPostProcessor.ModifiedSource, false);
      myLogger.Verbose("ModifiedSource: {0} EditorApplication.isPlaying: {1} EditorPrefsWrapper.AutoRefresh: {2}",
        modifiedSource, EditorApplication.isPlaying, EditorPrefsWrapper.AutoRefresh);

      if (modifiedSource && !EditorApplication.isPlaying && EditorPrefsWrapper.AutoRefresh || !File.Exists(PluginEntryPoint.SlnFile))
      {
        UnityUtils.SyncSolution(); // added to handle opening file, which was just recently created.
        EditorPrefs.SetBool(ModificationPostProcessor.ModifiedSource, false);
      }

      var model = UnityEditorProtocol.Models.FirstOrDefault();
      if (model != null)
      {
        if (PluginEntryPoint.CheckConnectedToBackendSync(model))
        {
          myLogger.Verbose("Calling OpenFileLineCol: {0}, {1}, {2}", assetFilePath, line, column);

          if (model.RiderProcessId.HasValue())
            AllowSetForegroundWindow(model.RiderProcessId.Value);
          else
            AllowSetForegroundWindow();

          model.OpenFileLineCol.Start(myLifetime, new RdOpenFileArgs(assetFilePath, line, column));

          // todo: maybe fallback to CallRider, if returns false
          return true;
        }
      }

      var argsString = assetFilePath == "" ? "" : $" --line {line} --column {column} \"{assetFilePath}\""; // on mac empty string in quotes is causing additional solution to be opened https://github.cds.internal.unity3d.com/unity/com.unity.ide.rider/issues/21
      var args = $"\"{mySlnFile}\"{argsString}";
      return CallRider(args);
    }

    public bool CallRider(string args)
    {
      var defaultApp = myRiderPathProvider.ValidateAndReturnActualRider(EditorPrefsWrapper.ExternalScriptEditor);
      if (string.IsNullOrEmpty(defaultApp))
      {
        myLogger.Verbose("Could not find default rider app");
        return false;
      }

      var proc = new Process();
      if (myPluginSettings.OSRider == OS.MacOSX)
      {
        proc.StartInfo.FileName = "open";
        proc.StartInfo.Arguments = $"-n \"{defaultApp}\" --args {args}";
      }
      else
      {
        proc.StartInfo.FileName = defaultApp;
        proc.StartInfo.Arguments = args;
      }
      proc.StartInfo.UseShellExecute = true; // avoid HandleInheritance
      var message = $"\"{proc.StartInfo.FileName}\" {proc.StartInfo.Arguments}";
      myLogger.Verbose(message);
      if (!proc.Start())
      {
        myLogger.Error($"Process failed to start. {message}");
        return false;
      }
      AllowSetForegroundWindow(proc.Id);
      return true;
    }

    // This is required to be called to help frontend Focus itself
    private void AllowSetForegroundWindow(int? processId=null)
    {
      if (myPluginSettings.OSRider != OS.Windows)
        return;

      try
      {
        var process = processId == null ? GetRiderProcess() : Process.GetProcessById((int)processId);
        if (process == null)
          return;

        if (process.Id > 0)
          User32Dll.AllowSetForegroundWindow(process.Id);
      }
      catch (Exception e)
      {
        myLogger.Warn("Exception on AllowSetForegroundWindow: " + e);
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
