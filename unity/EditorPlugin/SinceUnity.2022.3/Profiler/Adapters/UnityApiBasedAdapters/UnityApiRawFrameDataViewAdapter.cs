#nullable enable
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;
using UnityEditor.Profiling;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.UnityApiBasedAdapters
{
  public class UnityApiRawFrameDataViewAdapter : IRawFrameDataViewAdapter
  {
    private readonly RawFrameDataView myRawFrameDataView;

    public UnityApiRawFrameDataViewAdapter(RawFrameDataView rawFrameDataView)
    {
      myRawFrameDataView = rawFrameDataView;
    }

    public void Dispose()
    {
      myRawFrameDataView?.Dispose();
    }

    public double GetSampleTimeMs(int sampleIndex)
    {
      return myRawFrameDataView.GetSampleTimeMs(sampleIndex);
    }

    public int GetSampleMarkerId(int index)
    {
      return myRawFrameDataView.GetSampleMarkerId(index);
    }

    public int GetSampleChildrenCount(int index)
    {
      return myRawFrameDataView.GetSampleChildrenCount(index);
    }

    public string GetSampleName(int index)
    {
      return myRawFrameDataView.GetSampleName(index);
    }

    public int SampleCount => myRawFrameDataView.sampleCount;
    public int ThreadIndex => myRawFrameDataView.threadIndex;
    public string ThreadName => myRawFrameDataView.threadName;
    public double FrameStartTimeMs => myRawFrameDataView.frameStartTimeMs;
    public float FrameTimeMs => myRawFrameDataView.frameTimeMs;
  }
}