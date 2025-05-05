#nullable enable
using System.Linq;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;
using UnityEditor.Profiling;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.UnityApiBasedAdapters
{
  public class UnityApiRawFrameDataViewAdapter : IRawFrameDataViewAdapter
  {
    private readonly RawFrameDataView myRawFrameDataView;
    private readonly int myGCAllocMarkerId;

    public UnityApiRawFrameDataViewAdapter(RawFrameDataView rawFrameDataView)
    {
      myRawFrameDataView = rawFrameDataView;
      myGCAllocMarkerId = myRawFrameDataView.GetMarkerId("GC.Alloc");
    }

    public void Dispose()
    {
      myRawFrameDataView?.Dispose();
    }

    public bool Valid => myRawFrameDataView.valid;

    public long GetAllocSize(int sampleIndex)
    {
      if (myGCAllocMarkerId == -1)
        return 0;
      var sampleMetadataCount = myRawFrameDataView.GetSampleMetadataCount(sampleIndex);
      if(sampleMetadataCount == 0)
        return 0;
      
      var sampleMarkerId = GetSampleMarkerId(sampleIndex);
      return sampleMarkerId != myGCAllocMarkerId 
        ? 0 
        : myRawFrameDataView.GetSampleMetadataAsLong(sampleIndex, 0);
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