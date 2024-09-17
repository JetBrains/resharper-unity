using JetBrains.Lifetimes;
using JetBrains.Rider.Model.Unity.BackendUnity;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.Profiler
{
  public static class ProfilerWindowEventsHandler
  {
    private static EditorWindow ourProfilerWindow;

    private static IProfilerWindowSelectionDataProvider ProfilerWindowDataProvider =>
      ProfilerWindowFacade.Instance.IsSupportingCurrentUnityVersion
        ? ProfilerWindowFacade.Instance
        : null;

    private static void InternalUpdate()
    {
      UpdateFocusedWindow();
    }

    private static void UpdateFocusedWindow()
    {
      if (ProfilerWindowDataProvider == null)
        return;

      var profilerWindow = TryGetProfilerWindow();

      if (ourProfilerWindow != profilerWindow)
      {
        if (ourProfilerWindow != null)
        {
          ProfilerWindowDataProvider.Deinit(ourProfilerWindow, OnTimeSampleSelected);
          ourProfilerWindow = null;
        }

        if (profilerWindow != null)
          ProfilerWindowDataProvider.Init(profilerWindow, OnTimeSampleSelected);

        if (ProfilerWindowDataProvider.IsInitialized)
          ourProfilerWindow = profilerWindow;
      }
    }

    private static EditorWindow TryGetProfilerWindow()
    {
      var focusedWindow = EditorWindow.focusedWindow;
      return focusedWindow?.GetType() == ReflectionDataProvider.OurProfilerWindowReflectionData.ProfilerWindowType
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
      if (!ReflectionDataProvider.IsCompatibleWithCurrentUnityVersion)
        return;
      lifetime.Bracket(() => EditorApplication.update += InternalUpdate,
        () => { EditorApplication.update -= InternalUpdate; });
    }
  }
}