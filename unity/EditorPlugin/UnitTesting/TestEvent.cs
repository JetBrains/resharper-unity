namespace JetBrains.Rider.Unity.Editor.UnitTesting
{
  public enum EventType { TestStarted, TestFinished, RunFinished }

  public class TestEvent
  {
    public readonly EventType myType;
    public readonly object myTestEvent;

    public TestEvent(EventType type, object testEvent)
    {
      myType = type;
      myTestEvent = testEvent;
    }
  }
}