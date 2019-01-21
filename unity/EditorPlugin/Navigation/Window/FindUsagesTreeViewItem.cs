namespace JetBrains.Rider.Unity.Editor.Navigation.Window
{
  internal class FindUsagesTreeViewItem : FindUsagePathElement
  {
    public AbstractUsageElement UsageElement { get; }

    public FindUsagesTreeViewItem(int id, AbstractUsageElement sceneElement) : base(id)
    {
      UsageElement = sceneElement;
    } 
  }
}