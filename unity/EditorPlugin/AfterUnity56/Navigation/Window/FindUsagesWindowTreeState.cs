using System;
using System.Collections.Generic;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.Util;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.Navigation.Window
{
  // TODO : backend should pass group for each find usage result, frontend (unity) should react for changes and creat grouping nodes per group
  // and add actual result
  [Serializable]
  internal class FindUsagesWindowTreeState : TreeViewState
  {
    [SerializeField] 
    public List<SceneElement> SceneElements = new List<SceneElement>();

    [SerializeField] 
    public List<PrefabElement> PrefabElements = new List<PrefabElement>();
    
    [SerializeField] 
    public List<ScriptableObjectElement> ScriptableObjectElements = new List<ScriptableObjectElement>();
    
    public FindUsagesWindowTreeState()
    {
      
    }
    
    public FindUsagesWindowTreeState(AssetFindUsagesResultBase[] requests)
    {
      foreach (var request in requests)
      {
        if (request is HierarchyFindUsagesResult hierarchyFindUsagesResult)
        {
          if (request.FilePath.EndsWith(".prefab"))
          {
            PrefabElements.Add(new PrefabElement(request.FilePath, request.FileName, hierarchyFindUsagesResult.PathElements,
              hierarchyFindUsagesResult.RootIndices));
          }
          else
          {
            SceneElements.Add(new SceneElement(request.FilePath, request.FileName, hierarchyFindUsagesResult.PathElements,
              hierarchyFindUsagesResult.RootIndices));
          }
        }
        else
        {
          ScriptableObjectElements.Add(new ScriptableObjectElement(request.FilePath, request.FileName, EmptyArray<string>.Instance, EmptyArray<int>.Instance));
        }
      }
    }
  }
}