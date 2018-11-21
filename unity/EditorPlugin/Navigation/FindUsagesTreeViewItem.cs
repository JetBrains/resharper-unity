using UnityEditor.IMGUI.Controls;

namespace JetBrains.Rider.Unity.Editor.Navigation
{
  internal class FindUsagesTreeViewItem : FindUsagePathElement
  {
    public AbstractUsageElement UsageElement { get; }

    public FindUsagesTreeViewItem(AbstractUsageElement sceneElement)
    {
      UsageElement = sceneElement;
    } 
  }
}