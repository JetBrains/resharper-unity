using JetBrains.Lifetimes;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.Profiler
{
  public static class ProfilerWindowEventsHandler
  {
    private static EditorWindow ourProfilerWindow;
    private static IProfilerWindowSelectionDataProvider ourProfilerWindowDataProvider; 
    private static ReflectionDataProvider ourReflectionDataProvider;

    private static void InternalUpdate()
    {
      UpdateFocusedWindow();
    }

    private static void UpdateFocusedWindow()
    {
      if (ourProfilerWindowDataProvider == null)
      {
        ourReflectionDataProvider = new ReflectionDataProvider();
        ourProfilerWindowDataProvider = new ProfilerWindowFacade(ourReflectionDataProvider);
      }
      
      if (!ourProfilerWindowDataProvider.IsSupportingCurrentUnityVersion)
        return;

      var profilerWindow = TryGetProfilerWindow();

      if (ourProfilerWindow != profilerWindow)
      {
        DeinitCurrentProfilerWindowEventHandling();

        if (profilerWindow != null)
          ourProfilerWindowDataProvider.Init(profilerWindow, OnTimeSampleSelected);

        if (ourProfilerWindowDataProvider.IsInitialized)
          ourProfilerWindow = profilerWindow;
      }
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
      var requestSent = false;
      foreach (var model in UnityEditorProtocol.Models)
      {
        if (PluginEntryPoint.CheckConnectedToBackendSync(model))
        {
          model.OpenFileBySampleInfo.Start(new SampleStackInfo(sampleName, callStack));
          requestSent = true;
        }
      }

      //In case if no rider instance is running - just open solution
      if (!requestSent)
        PluginEntryPoint.OpenAssetHandler.OnOpenedAsset("", 0);
    }

    public static void Initialize(Lifetime lifetime)
    {
      lifetime.Bracket(() => EditorApplication.update += InternalUpdate,
        () =>
        {
          EditorApplication.update -= InternalUpdate;
          DeinitCurrentProfilerWindowEventHandling();
        });
    }
  }
}