using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;

namespace JetBrains.Rider.Unity.Editor.FindUsages.Window
{
  internal class FindUsagePathElement :
#if UNITY_CORCLR_OR_NEWER
    TreeViewItem<int>
#else
    TreeViewItem
#endif
  {
    private readonly int myChildId;

    public FindUsagePathElement(int childId)
    {
      myChildId = childId;
    }

    private readonly Dictionary<int, FindUsagePathElement> myChildren = new Dictionary<int, FindUsagePathElement>();

    public FindUsagePathElement CreateChild(FindUsagePathElement item)
    {
      myChildren[item.myChildId] = item;
      AddChild(item);
      return item;
    }

    public FindUsagePathElement GetChild(int childId)
    {
      myChildren.TryGetValue(childId, out var result);
      return result;
    }

    public bool HasChild(int childId)
    {
      return myChildren.ContainsKey(childId);
    }
  }
}