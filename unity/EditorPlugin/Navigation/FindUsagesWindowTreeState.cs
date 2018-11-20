using System.Collections.Generic;
using JetBrains.Platform.Unity.EditorPluginModel;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.Navigation
{
  internal class FindUsagesWindowTreeState : TreeViewState
  {
    // TODO prefabElement
    
    
    [SerializeField] internal List<SceneElement> SceneElements = new List<SceneElement>();

    public FindUsagesWindowTreeState()
    {
      
    }
    
    public FindUsagesWindowTreeState(RdFindUsageRequest[] requests)
    {
      foreach (var request in requests)
      {
        SceneElements.Add(new SceneElement(request.SceneName, request.Path, request.LocalId));
      }
      Debug.Log("Create tree state with size: " + SceneElements.Count);
    }
  }
}