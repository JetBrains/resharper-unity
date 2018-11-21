using System;
using System.Collections.Generic;
using JetBrains.Platform.Unity.EditorPluginModel;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.Navigation
{
  [Serializable]
  internal class FindUsagesWindowTreeState : TreeViewState
  {
    [SerializeField] 
    public List<SceneElement> SceneElements = new List<SceneElement>();

    [SerializeField] 
    public List<PrefabElement> PrefabElements = new List<PrefabElement>();
    
    public FindUsagesWindowTreeState()
    {
      
    }
    
    public FindUsagesWindowTreeState(RdFindUsageRequestBase[] requests)
    {
      foreach (var request in requests)
      {
        if (request is RdFindUsageRequestScene requestScene)
        {
          SceneElements.Add(new SceneElement(requestScene.FilePath, requestScene.PathElements, requestScene.LocalId));
        }
        
        if (request is RdFindUsageRequestPrefab requestPrefab)
        {
          PrefabElements.Add(new PrefabElement(requestPrefab.FilePath, requestPrefab.PathElements));
        }
      }
      Debug.Log("Create tree state with size: " + SceneElements.Count);
    }
  }
}