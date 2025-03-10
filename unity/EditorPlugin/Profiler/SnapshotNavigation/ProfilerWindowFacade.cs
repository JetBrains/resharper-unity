#nullable enable
using System;
using JetBrains.Diagnostics;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters.SnapshotNavigation;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.Profiler.SnapshotNavigation
{
  internal class ProfilerWindowFacade : IProfilerWindowSelectionDataProvider
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(ProfilerWindowFacade));
    private readonly IProfilerAdaptersFactory myAdaptersFactory;
    private readonly ReflectionDataProvider myReflectionDataProvider;
    private OnTimeSampleSelected? myOnTimeSampleSelected;
    private IProfilerDriverAdapter? myProfilerDriverAdapter;

    private ITreeViewControllerAdapter? myTreeViewControllerAdapter;

    internal ProfilerWindowFacade(ReflectionDataProvider reflectionDataProvider,
      IProfilerAdaptersFactory adaptersFactory)
    {
      myReflectionDataProvider = reflectionDataProvider;
      myAdaptersFactory = adaptersFactory;
      IsSupportingCurrentUnityVersion = myReflectionDataProvider.IsCompatibleWithCurrentUnityVersion;
    }

    public bool IsInitialized { get; private set; }
    public bool IsSupportingCurrentUnityVersion { get; }

    public void Deinit(EditorWindow? profilerWindow, OnTimeSampleSelected? onTimeSampleSelected)
    {
      if (!IsSupportingCurrentUnityVersion)
        return;

      if (!IsInitialized)
        return;

      try
      {
        if (myTreeViewControllerAdapter != null)
          myTreeViewControllerAdapter.ItemDoubleClicked -= MyTreeViewControllerAdapterOnItemDoubleClicked;
      }
      catch (Exception e)
      {
        ourLogger.Verbose($"Failed to init ProfilerWindowFacade: {e}");
      }
      finally
      {
        myOnTimeSampleSelected -= onTimeSampleSelected;
        IsInitialized = false;
        myTreeViewControllerAdapter = null;
      }
    }

    public void Init(EditorWindow profilerWindow, OnTimeSampleSelected? onTimeSampleSelected)
    {
      try
      {
        IsInitialized = InternalInit(profilerWindow);
        myOnTimeSampleSelected += onTimeSampleSelected;
        ourLogger.Verbose($"{nameof(ProfilerWindowFacade)} has been initialized.");
      }
      catch (Exception e)
      {
        ourLogger.Verbose($"Failed to init {nameof(ProfilerWindowFacade)}: {e}");
        IsInitialized = false;
      }
    }

    private bool InternalInit(EditorWindow profilerWindow)
    {
      myProfilerDriverAdapter = new ProfilerDriverAdapter(myReflectionDataProvider.ProfilerDriverReflectionData);
      if (myProfilerDriverAdapter == null)
      {
        ourLogger.Verbose($"Failed to create {nameof(ProfilerDriverAdapter)} for {nameof(ProfilerWindowFacade)}");
        return false;
      }

      myTreeViewControllerAdapter = myAdaptersFactory.TryCreateTreeViewControllerAdapter(profilerWindow);

      if (myTreeViewControllerAdapter == null)
        return false;

      myTreeViewControllerAdapter.ItemDoubleClicked += MyTreeViewControllerAdapterOnItemDoubleClicked;
      return true;
    }


    private void MyTreeViewControllerAdapterOnItemDoubleClicked(int obj)
    {
      if (myProfilerDriverAdapter == null)
      {
        ourLogger.Verbose(
          $"Can't handle double click on {nameof(ProfilerWindowFacade)}: {nameof(myProfilerDriverAdapter)} is null.");
        return;
      }

      try
      {
        var selectedPropertyPath = myProfilerDriverAdapter.GetSelectedPropertyPath() ?? string.Empty;
        myOnTimeSampleSelected?.Invoke(selectedPropertyPath, selectedPropertyPath);
      }
      catch (Exception e)
      {
        ourLogger.Verbose($"Failed to handle double click on {nameof(ProfilerWindowFacade)}: {e}");
      }
    }
  }
}