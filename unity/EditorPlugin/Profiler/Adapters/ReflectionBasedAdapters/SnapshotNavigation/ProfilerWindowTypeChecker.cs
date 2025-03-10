#nullable enable
using System;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters.SnapshotNavigation
{
  internal class ProfilerWindowTypeChecker : IProfilerWindowTypeChecker
  {
    private readonly ProfilerWindowReflectionData? myProfilerWindowReflectionData;

    public ProfilerWindowTypeChecker(ProfilerWindowReflectionData? profilerWindowReflectionData)
    {
      myProfilerWindowReflectionData = profilerWindowReflectionData;
    }

    public Object[] FindProfilerWindows()
    {
      return myProfilerWindowReflectionData == null
        ? Array.Empty<Object>()
        : Resources.FindObjectsOfTypeAll(myProfilerWindowReflectionData.ProfilerWindowType);
    }

    public bool IsProfilerWindow(object? profilerWindow)
    {
      if (profilerWindow == null)
        return false;
      return myProfilerWindowReflectionData?.ProfilerWindowType == profilerWindow.GetType();
    }
  }
}