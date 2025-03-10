#nullable enable
using System;
using JetBrains.Diagnostics;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters.SnapshotAnalysis
{
  internal class RawFrameDataViewAdapter : IRawFrameDataViewAdapter
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(RawFrameDataViewAdapter));
    private readonly object? myRawFrameDataViewObject;
    private readonly RawFrameDataViewReflectionData? myReflectionData;

    internal RawFrameDataViewAdapter(object? rawFrameDataViewObject, RawFrameDataViewReflectionData? reflectionData)
    {
      myRawFrameDataViewObject = rawFrameDataViewObject;
      myReflectionData = reflectionData;
      if (myReflectionData == null)
      {
        ourLogger.Verbose($"{nameof(myReflectionData)} is null.");
        return;
      }

      if (!myReflectionData.IsValid())
        ourLogger.Verbose($"{myReflectionData.GetType().Name} is not valid.");
    }

    public void Dispose()
    {
      try
      {
        var rawFrameDataViewObject = myRawFrameDataViewObject as IDisposable;
        rawFrameDataViewObject?.Dispose();
      }
      catch (Exception e)
      {
        ourLogger.Verbose($"{nameof(myRawFrameDataViewObject)} dispose error: {e}.");
      }
    }

    public double GetSampleTimeMs(int sampleIndex)
    {
      if (myReflectionData == null)
      {
        ourLogger.Verbose($"Can't get {nameof(GetSampleTimeMs)}: {nameof(myReflectionData)} is null.");
        return -1;
      }

      try
      {
        return (float)myReflectionData.GetSampleTimeMsMethod.Invoke(myRawFrameDataViewObject,
          new object[] { sampleIndex });
      }
      catch (Exception ex)
      {
        ourLogger.Verbose($"Error invoking {nameof(myReflectionData.GetSampleTimeMsMethod)}: {ex}");
        return -1;
      }
    }

    public int GetSampleMarkerId(int index)
    {
      if (myReflectionData == null)
      {
        ourLogger.Verbose($"Can't get {nameof(GetSampleMarkerId)}: {nameof(myReflectionData)} is null.");
        return -1;
      }

      try
      {
        return (int)myReflectionData.GetSampleMarkerIdMethod.Invoke(myRawFrameDataViewObject, new object[] { index });
      }
      catch (Exception ex)
      {
        ourLogger.Verbose("Failed to invoke GetSampleMarkerId", ex);
        return -1;
      }
    }

    public int GetSampleChildrenCount(int index)
    {
      if (myReflectionData == null)
      {
        ourLogger.Verbose($"Can't get {nameof(GetSampleChildrenCount)}: {nameof(myReflectionData)} is null.");
        return -1;
      }

      try
      {
        return (int)myReflectionData.GetSampleChildrenCountMethod.Invoke(myRawFrameDataViewObject,
          new object[] { index });
      }
      catch (Exception ex)
      {
        ourLogger.Verbose("Failed to invoke GetSampleChildrenCount", ex);
        return -1;
      }
    }

    public string GetSampleName(int index)
    {
      if (myReflectionData == null)
      {
        ourLogger.Verbose($"Can't get {nameof(GetSampleName)}: {nameof(myReflectionData)} is null.");
        return string.Empty;
      }

      try
      {
        return (string)myReflectionData.GetSampleNameMethod.Invoke(myRawFrameDataViewObject, new object[] { index });
      }
      catch (Exception ex)
      {
        ourLogger.Verbose("Failed to invoke GetSampleName", ex);
        return string.Empty;
      }
    }

    public double FrameStartTimeMs
    {
      get
      {
        if (myReflectionData == null)
        {
          ourLogger.Verbose($"Can't get {nameof(FrameStartTimeMs)}:  {nameof(myReflectionData)} is null.");
          return -1;
        }

        try
        {
          return (double)myReflectionData.FrameStartTimeMsProperty.GetValue(myRawFrameDataViewObject);
        }
        catch (Exception ex)
        {
          ourLogger.Verbose("Failed to access FrameStartTimeMsProperty", ex);
          return -1;
        }
      }
    }

    public float FrameTimeMs
    {
      get
      {
        if (myReflectionData == null)
        {
          ourLogger.Verbose($"Can't get {nameof(FrameTimeMs)}: {nameof(myReflectionData)} is null.");
          return -1;
        }

        try
        {
          return (float)myReflectionData.FrameTimeMsProperty.GetValue(myRawFrameDataViewObject);
        }
        catch (Exception ex)
        {
          ourLogger.Verbose("Failed to access FrameTimeMsProperty", ex);
          return -1;
        }
      }
    }

    public int SampleCount
    {
      get
      {
        if (myReflectionData == null)
        {
          ourLogger.Verbose($"Can't get {nameof(SampleCount)}: {nameof(myReflectionData)} is null.");
          return -1;
        }

        try
        {
          return (int)myReflectionData.SampleCountProperty.GetValue(myRawFrameDataViewObject);
        }
        catch (Exception ex)
        {
          ourLogger.Verbose("Failed to access SampleCountProperty", ex);
          return -1;
        }
      }
    }

    public string ThreadName
    {
      get
      {
        if (myReflectionData == null)
        {
          ourLogger.Verbose($"Can't get {nameof(ThreadName)}: {nameof(myReflectionData)} is null.");
          return string.Empty;
        }

        try
        {
          return (string)myReflectionData.ThreadNameProperty.GetValue(myRawFrameDataViewObject);
        }
        catch (Exception ex)
        {
          ourLogger.Verbose($"Failed to access {nameof(myReflectionData.ThreadNameProperty)}", ex);
          return string.Empty;
        }
      }
    }

    public int ThreadIndex
    {
      get
      {
        if (myReflectionData == null)
        {
          ourLogger.Verbose($"Can't get {nameof(ThreadIndex)}: {nameof(myReflectionData)} is null.");
          return -1;
        }

        try
        {
          return (int)myReflectionData.ThreadIndexProperty.GetValue(myRawFrameDataViewObject);
        }
        catch (Exception ex)
        {
          ourLogger.Verbose($"Failed to access {nameof(myReflectionData.ThreadIndexProperty)}", ex);
          return -1;
        }
      }
    }

    // Property invocations
    public float GetFrameStartTimeMs()
    {
      if (myReflectionData == null)
      {
        ourLogger.Verbose($"Can't get {nameof(GetFrameStartTimeMs)}: {nameof(myReflectionData)} is null.");
        return -1;
      }

      try
      {
        return (float)myReflectionData.FrameStartTimeMsProperty.GetValue(myRawFrameDataViewObject);
      }
      catch (Exception ex)
      {
        ourLogger.Verbose($"Error retrieving {nameof(myReflectionData.FrameStartTimeMsProperty)}: {ex}");
        return -1;
      }
    }
  }
}