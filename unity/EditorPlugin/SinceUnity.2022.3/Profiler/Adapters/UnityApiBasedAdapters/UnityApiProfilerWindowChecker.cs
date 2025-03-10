#nullable enable
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;
using UnityEditor;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.UnityApiBasedAdapters
{
  public class UnityApiProfilerWindowChecker : IProfilerWindowTypeChecker
  {
    public Object[] FindProfilerWindows()
    {
      return Resources.FindObjectsOfTypeAll<ProfilerWindow>();
    }

    public bool IsProfilerWindow(object? profilerWindow)
    {
      return profilerWindow?.GetType() == typeof(ProfilerWindow);
    }
  }
}