using System;
using JetBrains.Annotations;
using JetBrains.Diagnostics;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.SnapshotNavigation
{
  internal class TreeViewControllerAdapter
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(TreeViewControllerAdapter));
    private readonly TreeViewControllerReflectionData myReflectionData;
    private readonly object myTreeViewController;

    private TreeViewControllerAdapter(object treeViewController, TreeViewControllerReflectionData reflectionData)
    {
      myTreeViewController = treeViewController;
      myReflectionData = reflectionData;
    }

    [CanBeNull]
    public static TreeViewControllerAdapter Create(object treeViewController,
      TreeViewControllerReflectionData reflectionData)
    {
      if (!reflectionData.IsValid())
      {
        ourLogger.Verbose($"{reflectionData.GetType().Name} is not valid.");
        return null;
      }

      if (treeViewController?.GetType() != reflectionData.TreeViewControllerType)
      {
        ourLogger.Verbose($"Type '{TreeViewControllerReflectionData.TreeViewControllerTypeName}' expected.");
        return null;
      }

      return new TreeViewControllerAdapter(treeViewController, reflectionData);
    }

    private object GetDoubleClickedCallback()
    {
      return myReflectionData.ItemDoubleClickedCallbackPropertyInfo.GetValue(myTreeViewController);
    }

    private void SetDoubleClickedCallback(object value)
    {
      myReflectionData.ItemDoubleClickedCallbackPropertyInfo.SetValue(myTreeViewController, value);
    }

    private object GetContextClickItemCallback()
    {
      return myReflectionData.ContextClickItemCallback.GetValue(myTreeViewController);
    }

    private void SetContextClickItemCallback(object value)
    {
      myReflectionData.ContextClickItemCallback.SetValue(myTreeViewController, value);
    }


    public event Action<int> ItemDoubleClicked
    {
      add
      {
        var doubleClickedCallback = GetDoubleClickedCallback() as Action<int>;
        doubleClickedCallback += value;
        SetDoubleClickedCallback(doubleClickedCallback);
      }
      remove
      {
        var doubleClickedCallback = GetDoubleClickedCallback() as Action<int>;
        doubleClickedCallback -= value;
        SetDoubleClickedCallback(doubleClickedCallback);
      }
    }

    public event Action<int> ContextClickItem
    {
      add
      {
        var contextClickItemCallback = GetContextClickItemCallback() as Action<int>;
        contextClickItemCallback += value;
        SetContextClickItemCallback(contextClickItemCallback);
      }
      remove
      {
        var contextClickItemCallback = GetContextClickItemCallback() as Action<int>;
        contextClickItemCallback -= value;
        SetContextClickItemCallback(contextClickItemCallback);
      }
    }
  }
}