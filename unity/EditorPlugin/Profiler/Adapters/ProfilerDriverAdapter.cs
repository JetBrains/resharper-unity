using JetBrains.Diagnostics;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters
{
  internal static class ProfilerDriverAdapter
  {
    private static readonly ProfilerDriverReflectionData ourProfilerDriverReflectionData;
    private static readonly ILog ourLogger = Log.GetLog(nameof(ProfilerDriverAdapter));

    static ProfilerDriverAdapter()
    {
      ourProfilerDriverReflectionData = ReflectionDataProvider.OurProfilerDriverReflectionData;
      if (!ourProfilerDriverReflectionData.IsValid())
        ourLogger.Verbose($"{ourProfilerDriverReflectionData.GetType().Name} is not valid.");
    }

    public static string GetSelectedPropertyPath()
    {
      return ourProfilerDriverReflectionData.SelectedPropertyPathPropertyInfo.GetValue(null) as string;
    }
  }
}