#nullable enable
using System;
using JetBrains.Diagnostics;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.SnapshotAnalysis
{
  internal class RawFrameDataViewAdapter : IDisposable
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(RawFrameDataViewAdapter));
    private readonly RawFrameDataViewReflectionData myReflectionData;
    private readonly object? myRawFrameDataViewObject;

    private RawFrameDataViewAdapter(object? rawFrameDataViewObject, RawFrameDataViewReflectionData reflectionData)
    {
      myRawFrameDataViewObject = rawFrameDataViewObject;
      this.myReflectionData = reflectionData;

      if (!this.myReflectionData.IsValid())
        ourLogger.Verbose($"{this.myReflectionData.GetType().Name} is not valid.");
    }

    public static RawFrameDataViewAdapter? Create(object? rawFrameDataViewObject,
      RawFrameDataViewReflectionData reflectionData)
    {
      if (!reflectionData.IsValid())
      {
        ourLogger.Verbose($"{reflectionData.GetType().Name} is not valid.");
        return null;
      }

      if (rawFrameDataViewObject == null)
      {
        ourLogger.Verbose($"{reflectionData.GetType().Name} object is null");
        return null;
      }

      return new RawFrameDataViewAdapter(rawFrameDataViewObject, reflectionData);
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

    // Property invocations
    public float GetFrameStartTimeMs()
    {
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

    public int GetSampleMarkerId(int index)
    {
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
  }
}