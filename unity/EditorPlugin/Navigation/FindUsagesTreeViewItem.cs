using UnityEditor.IMGUI.Controls;

namespace JetBrains.Rider.Unity.Editor.Navigation
{
  internal class FindUsagesTreeViewItem : FindUsagePathElement
  {
    public SceneElement SceneElement { get; }

    public FindUsagesTreeViewItem(SceneElement sceneElement)
    {
      SceneElement = sceneElement;
    } 
  }
}