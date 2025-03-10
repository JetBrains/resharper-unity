#nullable enable
namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces
{
  public interface IProfilerSnapshotDriverAdapter
  {
    IRawFrameDataViewAdapter? GetRawFrameDataView(int frameIndex, int threadIndex);
  }
}