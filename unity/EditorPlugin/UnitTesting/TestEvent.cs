using System;
using JetBrains.Platform.Unity.EditorPluginModel;

namespace JetBrains.Rider.Unity.Editor.UnitTesting
{
  [Serializable]
  public enum EventType { TestStarted, TestFinished, RunFinished }

  [Serializable]
  public class TestEvent
  {
    public EventType myType;
    public TestInternalEvent Event;
    
    public TestEvent(EventType type, TestInternalEvent tEvent)
    {
      myType = type;
      Event = tEvent;
    }
  }
  
  [Serializable]
  public class TestInternalEvent
  {
    public string myID;
    public string myOutput;
    public Status myStatus;
    public int myDuration;
    public string myParentID;

    public TestInternalEvent(string id, string output, int duration, Status status, string parentID)
    {
      myID = id;
      myOutput = output;
      myStatus = status;
      myDuration = duration;
      myParentID = parentID;
    }
  }
}