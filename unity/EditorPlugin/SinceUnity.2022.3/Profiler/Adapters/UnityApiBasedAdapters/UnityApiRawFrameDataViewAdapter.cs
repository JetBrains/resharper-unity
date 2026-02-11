#nullable enable
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;
using UnityEditor.Profiling;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.UnityApiBasedAdapters
{
  public class UnityApiRawFrameDataViewAdapter : IRawFrameDataViewAdapter
  {
    private readonly RawFrameDataView? myRawFrameDataView;
    private readonly int myGCAllocMarkerId;

    public UnityApiRawFrameDataViewAdapter(RawFrameDataView rawFrameDataView)
    {
      if (rawFrameDataView.valid)
      {
        myRawFrameDataView = rawFrameDataView;
        myGCAllocMarkerId = rawFrameDataView.GetMarkerId("GC.Alloc");
      }
      else
      {
        myRawFrameDataView = null;
        myGCAllocMarkerId = -1;
      }
    }

    public void Dispose()
    {
      myRawFrameDataView?.Dispose();
    }

    public bool Valid => myRawFrameDataView?.valid ?? false;

    public long GetAllocSize(int sampleIndex)
    {
      if (myGCAllocMarkerId == -1 || myRawFrameDataView == null)
        return 0;
      var sampleMetadataCount = myRawFrameDataView.GetSampleMetadataCount(sampleIndex);
      if (sampleMetadataCount == 0)
        return 0;

      var sampleMarkerId = GetSampleMarkerId(sampleIndex);
      return sampleMarkerId != myGCAllocMarkerId
        ? 0
        : myRawFrameDataView.GetSampleMetadataAsLong(sampleIndex, 0);
    }

    public double GetSampleTimeMs(int sampleIndex)
    {
      return myRawFrameDataView?.GetSampleTimeMs(sampleIndex) ?? -1;
    }

    public int GetSampleMarkerId(int index)
    {
      return myRawFrameDataView?.GetSampleMarkerId(index) ?? -1;
    }

    public int GetSampleChildrenCount(int index)
    {
      return myRawFrameDataView?.GetSampleChildrenCount(index) ?? -1;
    }

    public string GetSampleName(int index)
    {
      return myRawFrameDataView?.GetSampleName(index) ?? string.Empty;
    }

    public int SampleCount => myRawFrameDataView?.sampleCount ?? -1;
    public int ThreadIndex => myRawFrameDataView?.threadIndex ?? -1;
    public string ThreadName => myRawFrameDataView?.threadName ?? string.Empty;
    public double FrameStartTimeMs => myRawFrameDataView?.frameStartTimeMs ?? -1;
    public float FrameTimeMs => myRawFrameDataView?.frameTimeMs ?? -1;
    public ulong FrameTimeNs => myRawFrameDataView?.frameTimeNs ?? 0;
  }
}