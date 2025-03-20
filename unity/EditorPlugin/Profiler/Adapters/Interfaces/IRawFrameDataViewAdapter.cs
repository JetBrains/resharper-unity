#nullable enable
using System;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces
{
  public interface IRawFrameDataViewAdapter : IDisposable
  {
    int SampleCount { get; }
    int ThreadIndex { get; }
    string ThreadName { get; }
    double FrameStartTimeMs { get; }
    float FrameTimeMs { get; }
    bool Valid { get; }
    double GetSampleTimeMs(int sampleIndex);
    int GetSampleMarkerId(int index);
    int GetSampleChildrenCount(int index);
    string GetSampleName(int index);
  }
}