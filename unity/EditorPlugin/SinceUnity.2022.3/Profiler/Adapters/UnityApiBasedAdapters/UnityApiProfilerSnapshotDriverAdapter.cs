#nullable enable
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;
using UnityEditorInternal;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.UnityApiBasedAdapters
{
  public class UnityApiProfilerSnapshotDriverAdapter : IProfilerSnapshotDriverAdapter
  {
    public IRawFrameDataViewAdapter? GetRawFrameDataView(int frameIndex, int threadIndex)
    {
      var rawFrameDataView = ProfilerDriver.GetRawFrameDataView(frameIndex, threadIndex);
      return new UnityApiRawFrameDataViewAdapter(rawFrameDataView);
    }
  }
}