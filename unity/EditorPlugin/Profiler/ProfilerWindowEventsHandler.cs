#nullable enable
using System;
using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Lifetimes;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Rider.PathLocator;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;
using JetBrains.Rider.Unity.Editor.Profiler.SnapshotAnalysis;
using JetBrains.Rider.Unity.Editor.Profiler.SnapshotNavigation;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JetBrains.Rider.Unity.Editor.Profiler
{
  public static class ProfilerWindowEventsHandler
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(ProfilerWindowEventsHandler));

    private static IProfilerAdaptersFactory? ourProfilerAdaptersFactory;
    private static IProfilerWindowTypeChecker? ourProfilerWindowTypeChecker;

    private static EditorWindow? ourLastProfilerWindow;
    private static EditorWindow? ourProfilerWindow;

    private static IProfilerWindowSelectionDataProvider? ourProfilerWindowDataProvider;
    private static ISnapshotCollectorDaemon? ourSnapshotCollectorDaemon;

    private static readonly ISignal<SampleStackInfo> ourOnTimeSampleSelectedSignal = new SignalBase<SampleStackInfo>();

    private static void InternalUpdate()
    {
      if (ourProfilerWindowDataProvider is not { IsSupportingCurrentUnityVersion: true })
        return;

      UpdateFocusedProfilerWindow();

      ourSnapshotCollectorDaemon?.Update(ourLastProfilerWindow);
    }

    private static void UpdateFocusedProfilerWindow()
    {
      var profilerWindow = TryGetProfilerWindow(EditorWindow.focusedWindow);
#if UNITY_2022_3_OR_NEWER
      if (!EditorApplication.isFocused && ourProfilerWindow == null)
        return;
#else
      if(EditorWindow.focusedWindow == null && ourProfilerWindow == null)
        return;
#endif

      if (ourProfilerWindow != profilerWindow)
      {
        DeinitCurrentProfilerWindowEventHandling();

        if (profilerWindow != null)
          ourProfilerWindowDataProvider?.Init(profilerWindow, OnTimeSampleSelected);

        if (ourProfilerWindowDataProvider is { IsInitialized: true })
          ourProfilerWindow = profilerWindow;
      }

      if (ourProfilerWindow != null)
        ourLastProfilerWindow = ourProfilerWindow;
    }

    private static void DeinitCurrentProfilerWindowEventHandling()
    {
      ourLogger.Verbose("DeinitCurrentProfilerWindowEventHandling");
      if (ourProfilerWindow != null)
      {
        ourProfilerWindowDataProvider?.Deinit(ourProfilerWindow, OnTimeSampleSelected);
        ourProfilerWindow = null;
      }
    }

    private static EditorWindow? TryGetProfilerWindow(EditorWindow? focusedWindow)
    {
      return ourProfilerWindowTypeChecker?.IsProfilerWindow(focusedWindow) == true
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
          ourProfilerAdaptersFactory =
#if UNITY_2022_3_OR_NEWER
            new JetBrains.Rider.Unity.Editor.Profiler.Adapters.UnityApiBasedAdapters.UnityApiBasedFactory();
#else
            new JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters.ReflectionBasedAdaptersFactory();
#endif
          ourLogger.Verbose("ProfilerWindowEventsHandler.Initialize");
          ourProfilerWindowDataProvider = ourProfilerAdaptersFactory!.CreateProfilerWindowFacade();
          ourProfilerWindowTypeChecker = ourProfilerAdaptersFactory!.CreateProfilerWindowTypeChecker();

          ourSnapshotCollectorDaemon = new SnapshotCollectorDaemon(ourProfilerAdaptersFactory, appDomainLifetime);

          //find an already opened profiler window
          var profilerWindowObjects = ourProfilerWindowTypeChecker?.FindProfilerWindows() ?? Array.Empty<Object>();
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