#nullable enable
using System;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces
{
  public interface ITreeViewControllerAdapter
  {
    event Action<int> ItemDoubleClicked;
    event Action<int> ContextClickItem;
  }
}