using JetBrains.Diagnostics;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters
{
  internal class ProfilerDriverAdapter
  {
    private readonly ProfilerDriverReflectionData myProfilerDriverReflectionData;
    private static readonly ILog ourLogger = Log.GetLog(nameof(ProfilerDriverAdapter));

    internal ProfilerDriverAdapter(ProfilerDriverReflectionData profilerDriverReflectionData)
    {
      myProfilerDriverReflectionData = profilerDriverReflectionData;
      if (!myProfilerDriverReflectionData.IsValid())
        ourLogger.Verbose($"{myProfilerDriverReflectionData.GetType().Name} is not valid.");
    }

    public string GetSelectedPropertyPath()
    {
      return myProfilerDriverReflectionData.SelectedPropertyPathPropertyInfo.GetValue(null) as string;
    }
  }
}