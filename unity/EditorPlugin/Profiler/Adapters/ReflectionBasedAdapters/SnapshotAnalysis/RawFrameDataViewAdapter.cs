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
    private readonly int? myGCAllocMarkerId;

    // Cached reflection parameter arrays for common method calls
    private readonly object[] mySingleIntParameterArray = new object[1];
    private readonly object[] myTwoIntParametersArray = new object[2];

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
      {
        ourLogger.Verbose($"{myReflectionData.GetType().Name} is not valid.");
        return;
      }

      // Initialize GC allocation marker ID in constructor
      try
      {
        myGCAllocMarkerId = (int)myReflectionData.GetMarkerIdMethod.Invoke(myRawFrameDataViewObject, new object[] { "GC.Alloc" });
      }
      catch (Exception ex)
      {
        ourLogger.Verbose($"Failed to initialize GC allocation marker ID: {ex}");
        myGCAllocMarkerId = -1;
      }
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
        mySingleIntParameterArray[0] = sampleIndex;
        return (float)myReflectionData.GetSampleTimeMsMethod.Invoke(myRawFrameDataViewObject, mySingleIntParameterArray);
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
        mySingleIntParameterArray[0] = index;
        return (int)myReflectionData.GetSampleMarkerIdMethod.Invoke(myRawFrameDataViewObject, mySingleIntParameterArray);
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
        mySingleIntParameterArray[0] = index;
        return (int)myReflectionData.GetSampleChildrenCountMethod.Invoke(myRawFrameDataViewObject, mySingleIntParameterArray);
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

    public bool Valid
    {
      get
      {
        if (myReflectionData == null)
        {
          ourLogger.Verbose($"Can't get {nameof(Valid)}: {nameof(myReflectionData)} is null.");
          return false;
        }

        try
        {
          return (bool)myReflectionData.ValidProperty.GetValue(myRawFrameDataViewObject);
        }
        catch (Exception ex)
        {
          ourLogger.Verbose($"Failed to access {nameof(myReflectionData.ValidProperty)}", ex);
          return false;
        }
      }
    }

    public long GetAllocSize(int sampleIndex)
    {
      if (myReflectionData == null || !myGCAllocMarkerId.HasValue)
      {
        ourLogger.Verbose($"Can't get {nameof(GetAllocSize)}: required data is not available.");
        return 0;
      }

      // Early return if GC allocation marker ID is not found
      if (myGCAllocMarkerId.Value == -1)
        return 0;

      try
      {
        // Get the sample marker ID first to avoid unnecessary metadata count check
        var sampleMarkerId = GetSampleMarkerId(sampleIndex);

        // Early return if the marker ID doesn't match the GC allocation marker ID
        if (sampleMarkerId != myGCAllocMarkerId.Value)
          return 0;

        // Check if the sample has metadata
        mySingleIntParameterArray[0] = sampleIndex;
        var sampleMetadataCount = (int)myReflectionData.GetSampleMetadataCountMethod.Invoke(myRawFrameDataViewObject, mySingleIntParameterArray);
        if (sampleMetadataCount == 0)
          return 0;

        // Get the allocation size
        myTwoIntParametersArray[0] = sampleIndex;
        myTwoIntParametersArray[1] = 0;
        return (long)myReflectionData.GetSampleMetadataAsLongMethod.Invoke(myRawFrameDataViewObject, myTwoIntParametersArray);
      }
      catch (Exception ex)
      {
        ourLogger.Verbose($"Error in {nameof(GetAllocSize)}: {ex}");
        return 0;
      }
    }
  }
}
