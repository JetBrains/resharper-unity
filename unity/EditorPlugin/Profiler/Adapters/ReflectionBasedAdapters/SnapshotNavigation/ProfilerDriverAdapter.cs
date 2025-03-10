#nullable enable
using JetBrains.Diagnostics;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters.SnapshotNavigation
{
  internal class ProfilerDriverAdapter : IProfilerDriverAdapter
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(ProfilerDriverAdapter));
    private readonly ProfilerDriverReflectionData? myProfilerDriverReflectionData;

    internal ProfilerDriverAdapter(ProfilerDriverReflectionData? profilerDriverReflectionData)
    {
      myProfilerDriverReflectionData = profilerDriverReflectionData;
      if (myProfilerDriverReflectionData == null)
      {
        ourLogger.Verbose($"{nameof(myProfilerDriverReflectionData)} is null.");
        return;
      }

      if (!myProfilerDriverReflectionData.IsValid())
        ourLogger.Verbose($"{myProfilerDriverReflectionData.GetType().Name} is not valid.");
    }

    public string? GetSelectedPropertyPath()
    {
      if (myProfilerDriverReflectionData == null)
      {
        ourLogger.Verbose(
          $"Can't get {nameof(GetSelectedPropertyPath)}: {nameof(myProfilerDriverReflectionData)} is null.");
        return null;
      }

      if (myProfilerDriverReflectionData.SelectedPropertyPathPropertyInfo == null)
      {
        ourLogger.Verbose(
          $"Can't get {nameof(GetSelectedPropertyPath)}: {nameof(myProfilerDriverReflectionData.SelectedPropertyPathPropertyInfo)} is null.");
        return null;
      }

      return myProfilerDriverReflectionData?.SelectedPropertyPathPropertyInfo.GetValue(null) as string;
    }
  }
}