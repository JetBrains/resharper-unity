namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.SnapshotNavigation
{
  internal class TimeLineGUIAdapter
  {
    private readonly object myTimeLineGUI;

    private TimeLineGUIAdapter(object timeLineGUI)
    {
      myTimeLineGUI = timeLineGUI;
    }

    public static TimeLineGUIAdapter Create(object timeLineGUI)
    {
      return new TimeLineGUIAdapter(timeLineGUI);
    }
  }
}