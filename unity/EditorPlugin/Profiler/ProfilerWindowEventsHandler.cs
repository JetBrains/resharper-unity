using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Rider.PathLocator;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.SnapshotAnalysis;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.SnapshotNavigation;
using JetBrains.Rider.Unity.Editor.Profiler.SnapshotAnalysis;
using JetBrains.Rider.Unity.Editor.Profiler.SnapshotNavigation;
using UnityEditor;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.Profiler
{
  public static class ProfilerWindowEventsHandler
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(ProfilerWindowEventsHandler));
    private static ReflectionDataProvider ourReflectionDataProvider;
    private static SnapshotReflectionDataProvider ourSnapshotReflectionDataProvider;

    private static EditorWindow ourLastProfilerWindow;
    private static EditorWindow ourProfilerWindow;

    private static IProfilerWindowSelectionDataProvider ourProfilerWindowDataProvider;
    private static ISnapshotCollectorDaemon ourSnapshotCollectorDaemon;

    private static readonly ISignal<SampleStackInfo> ourOnTimeSampleSelectedSignal = new SignalBase<SampleStackInfo>();

    private static void InternalUpdate()
    {
      if (!ourProfilerWindowDataProvider.IsSupportingCurrentUnityVersion)
        return;

      UpdateFocusedProfilerWindow();

      if (!ourSnapshotReflectionDataProvider.IsCompatibleWithCurrentUnityVersion)
        return;

      ourSnapshotCollectorDaemon?.Update(ourLastProfilerWindow);
    }

    private static void UpdateFocusedProfilerWindow()
    {
      var profilerWindow = TryGetProfilerWindow();

      if (ourProfilerWindow != profilerWindow)
      {
        DeinitCurrentProfilerWindowEventHandling();

        if (profilerWindow != null)
          ourProfilerWindowDataProvider.Init(profilerWindow, OnTimeSampleSelected);

        if (ourProfilerWindowDataProvider.IsInitialized)
          ourProfilerWindow = profilerWindow;
      }

      if (ourProfilerWindow != null)
        ourLastProfilerWindow = ourProfilerWindow;
    }

    private static void DeinitCurrentProfilerWindowEventHandling()
    {
      if (ourProfilerWindow != null)
      {
        ourProfilerWindowDataProvider.Deinit(ourProfilerWindow, OnTimeSampleSelected);
        ourProfilerWindow = null;
      }
    }

    private static EditorWindow TryGetProfilerWindow()
    {
      var focusedWindow = EditorWindow.focusedWindow;
      return focusedWindow?.GetType() == ourReflectionDataProvider.ProfilerWindowReflectionData.ProfilerWindowType
        ? focusedWindow
        : null;
    }

    private static void OnTimeSampleSelected(string sampleName, string callStack)
    {
      ourLogger.Verbose($"OnTimeSampleSelected: {sampleName}, {callStack}");
      ourOnTimeSampleSelectedSignal.Fire(new SampleStackInfo(sampleName, callStack));
      //In case if no rider instance is running - just open solution,
      //this option is temporary turned off for better times 
      // PluginEntryPoint.OpenAssetHandler.OnOpenedAsset("", 0);
    }


    public static void Initialize(Lifetime appDomainLifetime)
    {
      appDomainLifetime.Bracket(() =>
        {
          ourLogger.Verbose("ProfilerWindowEventsHandler.Initialize");
          ourReflectionDataProvider = new ReflectionDataProvider();
          ourProfilerWindowDataProvider = new ProfilerWindowFacade(ourReflectionDataProvider);
          ourSnapshotReflectionDataProvider = new SnapshotReflectionDataProvider();
          ourSnapshotCollectorDaemon = new SnapshotCollectorDaemon(ourSnapshotReflectionDataProvider, ourReflectionDataProvider, appDomainLifetime);

          //find an already opened profiler window
          var profilerWindowObjects = Resources.FindObjectsOfTypeAll(ourReflectionDataProvider.ProfilerWindowReflectionData.ProfilerWindowType);
          ourLastProfilerWindow = profilerWindowObjects.Length > 0 ? profilerWindowObjects[0] as EditorWindow : null;

          EditorApplication.update += InternalUpdate;
        },
        () =>
        {
          ourLogger.Verbose("ProfilerWindowEventsHandler.Deinitialize");
          EditorApplication.update -= InternalUpdate;
          DeinitCurrentProfilerWindowEventHandling();
          ourSnapshotCollectorDaemon?.Deinit();
          ourSnapshotCollectorDaemon = null;
          ourLastProfilerWindow = null;
        });
    }

    //There could be multiple connections from different rider instances to single Unity editor
    public static void Advise(Lifetime connectionLifetime, UnityProfilerModel model,
      BackendUnityModel backendUnityModel)
    {
      ourLogger.Verbose("ProfilerWindowEventsHandler.Advise");
      ourOnTimeSampleSelectedSignal.Advise(connectionLifetime, info =>
      {
        var myOpener = new RiderFileOpener(RiderPathProvider.RiderPathLocator.RiderLocatorEnvironment);
        int? processId = backendUnityModel.RiderProcessId.HasValue() ? backendUnityModel.RiderProcessId.Value : null;
        myOpener.AllowSetForegroundWindow(processId);
          
        model.OpenFileBySampleInfo.Start(connectionLifetime, info);
      });
      ourSnapshotCollectorDaemon?.Advise(connectionLifetime, model);
    }
  }
}