using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using JetBrains.Core;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.Unity.Editor.Logger;
using UnityEditor;
using UnityEditor.Callbacks;
using Application = UnityEngine.Application;
using Debug = UnityEngine.Debug;

namespace JetBrains.Rider.Unity.Editor
{
  // DO NOT RENAME!
  // Used by package via reflection
  [InitializeOnLoad, PublicAPI]
  public static class PluginEntryPoint
  {
    private static readonly ILog ourLogger = Log.GetLog("RiderPlugin");
    private static readonly IPluginSettings ourPluginSettings;
    private static readonly RiderPathProvider ourRiderPathProvider;
    private static readonly long ourInitTime = DateTime.UtcNow.Ticks;

    internal static string SlnFile;

    // This an entry point
    static PluginEntryPoint()
    {
      if (UnityUtils.IsInBatchModeAndNotInRiderTests)
        return;

      var lifetimeDefinition = Lifetime.Define(Lifetime.Eternal);
      AppDomain.CurrentDomain.DomainUnload += (_, __) =>
      {
        ourLogger.Verbose("LifetimeDefinition.Terminate");
        lifetimeDefinition.Terminate();
      };

      // Init log before doing any logging (the log above is in a lambda)
      LogInitializer.InitLog(lifetimeDefinition.Lifetime, PluginSettings.SelectedLoggingLevel);

      // Start collecting Unity messages ASAP
      UnityEventLogSender.Start(lifetimeDefinition.Lifetime);

      ourPluginSettings = new PluginSettings();
      ourRiderPathProvider = new RiderPathProvider(ourPluginSettings);

      // Old mechanism, when EditorPlugin was copied to Assets folder. Package was introduced with Unity 2019.2
      AssetsBasedPlugin.Initialise(lifetimeDefinition.Lifetime, ourRiderPathProvider, ourPluginSettings, ourLogger);

      Init(lifetimeDefinition.Lifetime);
    }

    // DO NOT REMOVE!
    // This is used when the plugin is loaded explicitly by the package to ensure the static class constructor has been
    // executed. Note that the package doesn't call this method. See AfterUnity56.EntryPoint for more details
    internal static void EnsureInitialised()
    {
      // Do nothing. We just need the class constructor called
    }

    internal static bool CheckConnectedToBackendSync(BackendUnityModel model)
    {
      if (model == null)
        return false;
      var connected = false;
      try
      {
        // HostConnected also means that in Rider and in Unity the same solution is opened
        connected = model.IsBackendConnected.Sync(Unit.Instance,
          new RpcTimeouts(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(200)));
      }
      catch (Exception)
      {
        ourLogger.Verbose("Rider Protocol not connected.");
      }

      return connected;
    }

    public static bool CallRider(string args)
    {
      return OpenAssetHandler.CallRider(args);
    }

    public static bool IsRiderDefaultEditor()
    {
        if (UnityUtils.UseRiderTestPath)
            return true;

        // Regular check
        var defaultApp = EditorPrefsWrapper.ExternalScriptEditor;
        bool isEnabled = !string.IsNullOrEmpty(defaultApp) &&
                         Path.GetFileName(defaultApp).ToLower().Contains("rider") &&
                         !UnityUtils.IsInBatchModeAndNotInRiderTests;
        return isEnabled;
    }

    private static void Init(Lifetime lifetime)
    {
      var projectDirectory = Directory.GetParent(Application.dataPath).NotNull().FullName;
      var projectName = Path.GetFileName(projectDirectory);
      SlnFile = Path.GetFullPath($"{projectName}.sln");

      InitializeEditorInstanceJson(lifetime);
      PlayModeStateTracker.Initialise();
      UnityEditorProtocol.Initialise(lifetime, ourInitTime, ourLogger);

      OpenAssetHandler = new OnOpenAssetHandler(ourRiderPathProvider, ourPluginSettings, SlnFile);

      // Done
      if (PluginSettings.SelectedLoggingLevel >= LoggingLevel.VERBOSE)
      {
        var executingAssembly = Assembly.GetExecutingAssembly();
        var location = executingAssembly.Location;
        Debug.Log(
          $"Rider plugin \"{executingAssembly.GetName().Name}\" initialized{(string.IsNullOrEmpty(location) ? "" : " from: " + location)}. " +
          $"LoggingLevel: {PluginSettings.SelectedLoggingLevel}. Change it in Unity Preferences -> Rider. Logs path: {LogInitializer.LogPath}.");
      }
    }

    // DO NOT RENAME OR REFACTOR!
    // Accessed by package via reflection
    [PublicAPI, Obsolete("Use LogInitializer.LogPath")]
    internal static readonly string LogPath = LogInitializer.LogPath;

    // DO NOT RENAME OR REFACTOR!
    // Accessed by package via reflection
    [PublicAPI]
    internal static OnOpenAssetHandler OpenAssetHandler;

    // Creates and deletes Library/EditorInstance.json containing info about unity instance. Unity 2017.1+ writes this
    // file itself. We'll always overwrite, just to be sure it's up to date. The file contents are exactly the same
    private static void InitializeEditorInstanceJson(Lifetime lifetime)
    {
      if (UnityUtils.UnityVersion >= new Version(2017, 1))
        return;

      ourLogger.Verbose("Writing Library/EditorInstance.json");

      var editorInstanceJsonPath = Path.GetFullPath("Library/EditorInstance.json");

      File.WriteAllText(editorInstanceJsonPath, $@"{{
  ""process_id"": {Process.GetCurrentProcess().Id},
  ""version"": ""{UnityUtils.UnityApplicationVersion}""
}}");

      lifetime.OnTermination(() =>
      {
        ourLogger.Verbose("Deleting Library/EditorInstance.json");
        File.Delete(editorInstanceJsonPath);
      });
    }

    /// <summary>
    /// Called when Unity is about to open an asset. This method is for pre-2019.2
    /// </summary>
    [OnOpenAsset]
    static bool OnOpenedAsset(int instanceID, int line)
    {
      if (!IsRiderDefaultEditor())
        return false;

      // if (UnityUtils.UnityVersion >= new Version(2019, 2)
      //   return false;
      return OpenAssetHandler.OnOpenedAsset(instanceID, line, 0);
    }
  }
}
