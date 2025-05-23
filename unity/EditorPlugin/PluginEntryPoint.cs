using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using JetBrains.Annotations;
using JetBrains.Core;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.Unity.Editor.Profiler;
using Application = UnityEngine.Application;
using Debug = UnityEngine.Debug;
using UnityEditor.Build.Reporting;

namespace JetBrains.Rider.Unity.Editor
{
  // DO NOT CHANGE NAME OR NAMESPACE!
  // Used by package via reflection
  [PublicAPI]
  public static class PluginEntryPoint
  {
    private static readonly ILog ourLogger = Log.GetLog("RiderPlugin");
    private static readonly IPluginSettings ourPluginSettings = new PluginSettings();
    private static readonly RiderPathProvider ourRiderPathProvider = new RiderPathProvider();
    private static readonly long ourInitTime = DateTime.UtcNow.Ticks;
    private static bool ourInitialised;

    // DO NOT RENAME OR REFACTOR!
    // Accessed by package via reflection
    [PublicAPI, Obsolete("Use LogInitializer.LogPath")]
    internal static readonly string LogPath = LogInitializer.LogPath;

    // DO NOT RENAME OR REFACTOR!
    // Accessed by package via reflection
    [PublicAPI]
    internal static OnOpenAssetHandler OpenAssetHandler;
    
    // DO NOT RENAME OR REFACTOR!
    // Accessed by package via reflection
    [PublicAPI, Obsolete("Remove it later")]
    internal static string GenerateAdditionalLinkXmlFile(BuildReport report, object data,
      bool preserveUnityEngineDlls, bool preservePlayerDlls)
    {
      return string.Empty;
    }

    internal static string SlnFile;
    internal static Lifetime AppDomainLifetime;

    // DO NOT RENAME OR REFACTOR!
    /// <summary>
    /// For "Unity CoreCLR" EditorPlugin is initialized from the Rider package by reflection
    /// The Initialize itself is always sync, fully executed within ProcessInitializeOnLoadAttributes
    /// </summary>
    /// <param name="token">Is used as a root of Lifetimes, which are passed further to protocol, etc</param>
    [PublicAPI]
    internal static void Initialize(CancellationToken token)
    {
      if (ourInitialised || UnityUtils.IsInBatchModeAndNotInRiderTests)
        return;

      var lifetimeDefinition = Lifetime.Define(Lifetime.Eternal);
      token.Register(() =>
      {
        ourLogger.Verbose("Initialize. LifetimeDefinition.Terminate");
        lifetimeDefinition.Terminate();
      });

      AppDomainLifetime = lifetimeDefinition.Lifetime;

      // Init log before doing any logging, and start collecting Unity messages ASAP
      LogInitializer.InitLog(AppDomainLifetime, PluginSettings.SelectedLoggingLevel);
      UnityEventLogSender.Start(AppDomainLifetime);

      // ReSharper disable once PossibleNullReferenceException
      var projectName = Path.GetFileName(Directory.GetParent(Application.dataPath).FullName);
      SlnFile = Path.GetFullPath($"{projectName}.sln");

      CreateEditorInstanceJson(AppDomainLifetime);
      PlayModeStateTracker.Initialise();
      UnityEditorProtocol.Initialise(AppDomainLifetime, ourInitTime, ourLogger);

      OpenAssetHandler = new OnOpenAssetHandler(AppDomainLifetime, ourRiderPathProvider, SlnFile);

      ProfilerWindowEventsHandler.Initialize(AppDomainLifetime);
      ReportInitialisationDone();

      ourInitialised = true;
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
  }
}
