#nullable enable
using System;
using JetBrains.Diagnostics;
using JetBrains.Rider.Unity.Editor.Profiler.Adapters.Interfaces;

namespace JetBrains.Rider.Unity.Editor.Profiler.Adapters.ReflectionBasedAdapters.SnapshotNavigation
{
  internal class TreeViewControllerAdapter : ITreeViewControllerAdapter
  {
    private static readonly ILog ourLogger = Log.GetLog(nameof(TreeViewControllerAdapter));
    private readonly TreeViewControllerReflectionData? myReflectionData;
    private readonly object myTreeViewController;

    internal TreeViewControllerAdapter(object treeViewController, TreeViewControllerReflectionData? reflectionData)
    {
      myTreeViewController = treeViewController;
      myReflectionData = reflectionData;
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
        SetDoubleClickedCallback(doubleClickedCallback!);
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
        SetContextClickItemCallback(contextClickItemCallback!);
      }
    }

    private object? GetDoubleClickedCallback()
    {
      if (myReflectionData == null)
      {
        ourLogger.Verbose($"Can't get {nameof(GetDoubleClickedCallback)}: {nameof(myReflectionData)} is null.");
        return null;
      }

      if (myReflectionData.ItemDoubleClickedCallbackPropertyInfo == null)
      {
        ourLogger.Verbose(
          $"Can't get {nameof(GetDoubleClickedCallback)}: {nameof(myReflectionData.ItemDoubleClickedCallbackPropertyInfo)} is null.");
        return null;
      }

      return myReflectionData.ItemDoubleClickedCallbackPropertyInfo.GetValue(myTreeViewController);
    }

    private void SetDoubleClickedCallback(object value)
    {
      if (myReflectionData == null)
      {
        ourLogger.Verbose($"Can't call {nameof(SetDoubleClickedCallback)}: {nameof(myReflectionData)} is null.");
        return;
      }

      if (myReflectionData.ItemDoubleClickedCallbackPropertyInfo == null)
      {
        ourLogger.Verbose(
          $"Can't get {nameof(SetDoubleClickedCallback)}: {nameof(myReflectionData.ItemDoubleClickedCallbackPropertyInfo)} is null.");
        return;
      }

      myReflectionData.ItemDoubleClickedCallbackPropertyInfo.SetValue(myTreeViewController, value);
    }

    private object? GetContextClickItemCallback()
    {
      if (myReflectionData == null)
      {
        ourLogger.Verbose($"Can't get {nameof(GetContextClickItemCallback)}: {nameof(myReflectionData)} is null.");
        return null;
      }

      if (myReflectionData.ContextClickItemCallback == null)
      {
        ourLogger.Verbose(
          $"Can't get {nameof(GetContextClickItemCallback)}: {nameof(myReflectionData.ContextClickItemCallback)} is null.");
        return null;
      }

      return myReflectionData.ContextClickItemCallback.GetValue(myTreeViewController);
    }

    private void SetContextClickItemCallback(object value)
    {
      if (myReflectionData == null)
      {
        ourLogger.Verbose($"Can't call {nameof(SetContextClickItemCallback)}: {nameof(myReflectionData)} is null.");
        return;
      }

      if (myReflectionData.ContextClickItemCallback == null)
      {
        ourLogger.Verbose(
          $"Can't get {nameof(SetContextClickItemCallback)}: {nameof(myReflectionData.ContextClickItemCallback)} is null.");
        return;
      }

      myReflectionData.ContextClickItemCallback.SetValue(myTreeViewController, value);
    }
  }
}