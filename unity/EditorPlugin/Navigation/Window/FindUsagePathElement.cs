using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;

namespace JetBrains.Rider.Unity.Editor.Navigation
{
  internal class FindUsagePathElement : TreeViewItem
  {
    private readonly Dictionary<string, FindUsagePathElement> myChildren = new Dictionary<string, FindUsagePathElement>();

    public FindUsagePathElement CreateChild(FindUsagePathElement item)
    {
      myChildren[item.displayName] = item;
      AddChild(item);
      return item;
    }

    public FindUsagePathElement GetChild(string name)
    {
      myChildren.TryGetValue(name, out var result);
      return result;
    }
    
    public bool HasChild(string name)
    {
      return myChildren.ContainsKey(name);
    }
  }
}