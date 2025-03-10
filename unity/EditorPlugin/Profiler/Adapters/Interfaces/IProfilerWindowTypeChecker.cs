#nullable enable
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces
{
  public interface IProfilerWindowTypeChecker
  {
    Object[] FindProfilerWindows();
    bool IsProfilerWindow(object? profilerWindow);
  }
}