using System;

namespace JetBrains.Rider.Unity.Editor.AfterUnity56.UnitTesting
{
  [Serializable]
  public enum EventType { TestStarted, TestFinished, RunFinished }

  [Serializable]
  public class TestEvent
  {
    public EventType type;
    public string id;
    public string assemblyName;
    public string output;
    public string resultState;
    public double duration;
    public string parentID;
    
    public TestEvent(EventType type, string id, string assemblyName, string output, double duration, string resultState, string parentID)
    {
      this.type = type;
      this.id = id;
      this.assemblyName = assemblyName;
      this.output = output;
      this.resultState = resultState;
      this.duration = duration;
      this.parentID = parentID;
    }
  }
}