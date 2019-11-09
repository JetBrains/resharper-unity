using System;
using System.Collections.Generic;
using JetBrains.Rider.Model;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.Navigation.Window
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

    public FindUsagesWindowTreeState(RdFindUsageResultElement[] requests)
    {
      foreach (var request in requests)
      {
        if (request.IsPrefab)
        {
          PrefabElements.Add(new PrefabElement(request.FilePath, request.FileName, request.PathElements, request.RootIndices));
        }
        else
        {
          SceneElements.Add(new SceneElement(request.FilePath, request.FileName, request.PathElements, request.RootIndices));
        }
      }
    }
  }
}