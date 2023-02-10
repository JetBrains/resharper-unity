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
    private static readonly IPluginSettings ourPluginSettings = new PluginSettings();
    private static readonly RiderPathProvider ourRiderPathProvider = new RiderPathProvider(ourPluginSettings);
    private static readonly long ourInitTime = DateTime.UtcNow.Ticks;

    // DO NOT RENAME OR REFACTOR!
    // Accessed by package via reflection
    [PublicAPI, Obsolete("Use LogInitializer.LogPath")]
    internal static readonly string LogPath = LogInitializer.LogPath;

    // DO NOT RENAME OR REFACTOR!
    // Accessed by package via reflection
    [PublicAPI]
    internal static readonly OnOpenAssetHandler OpenAssetHandler;

    internal static readonly string SlnFile;

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

      var appDomainLifetime = lifetimeDefinition.Lifetime;

      // Init log before doing any logging, and start collecting Unity messages ASAP
      LogInitializer.InitLog(appDomainLifetime, PluginSettings.SelectedLoggingLevel);
      UnityEventLogSender.Start(appDomainLifetime);

      // Old mechanism, when EditorPlugin was copied to Assets folder. Package was introduced with Unity 2019.2
      AssetsBasedPlugin.Initialise(appDomainLifetime, ourRiderPathProvider, ourPluginSettings, ourLogger);

      // ReSharper disable once PossibleNullReferenceException
      var projectName = Path.GetFileName(Directory.GetParent(Application.dataPath).FullName);
      SlnFile = Path.GetFullPath($"{projectName}.sln");

      CreateEditorInstanceJson(appDomainLifetime);
      PlayModeStateTracker.Initialise();
      UnityEditorProtocol.Initialise(appDomainLifetime, ourInitTime, ourLogger);

      OpenAssetHandler = new OnOpenAssetHandler(ourRiderPathProvider, ourPluginSettings, SlnFile);

      ReportInitialisationDone();
    }

    // DO NOT REMOVE!
    // This is used when the plugin is loaded explicitly by the package to ensure the static class constructor has been
    // executed. Note that the package doesn't call this method. See AfterUnity56.EntryPoint for more details
    internal static void EnsureInitialised()
    {
      // Do nothing. Don't move initialisation here - the class constructor is always called, either by InitializeOnLoad
      // or when EntryPoint calls this method. We either leave initialisation in the class constructor or introduce a
      // flag to prevent initialising twice. Might as well just leave it in the cctor.
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

    // Creates and deletes Library/EditorInstance.json containing info about unity instance. Unity 2017.1+ writes this
    // file itself. We'll always overwrite, just to be sure it's up to date. The file contents are exactly the same
    private static void CreateEditorInstanceJson(Lifetime lifetime)
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

    private static void ReportInitialisationDone()
    {
      if (PluginSettings.SelectedLoggingLevel >= LoggingLevel.VERBOSE)
      {
        var executingAssembly = Assembly.GetExecutingAssembly();
        var location = executingAssembly.Location;
        Debug.Log(
          $"Rider plugin \"{executingAssembly.GetName().Name}\" initialized{(string.IsNullOrEmpty(location) ? "" : " from: " + location)}. " +
          $"LoggingLevel: {PluginSettings.SelectedLoggingLevel}. Change it in Unity Preferences -> Rider. Logs path: {LogInitializer.LogPath}.");
      }
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
