using System;
using System.Collections.Generic;
using JetBrains.Rider.Model.Unity.BackendUnity;
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

    [SerializeField]
    public List<AnimatorElement> AnimatorElements = new List<AnimatorElement>();
    
    [SerializeField]
    public List<AnimationElement> AnimationElements = new List<AnimationElement>();

    public FindUsagesWindowTreeState()
    {

    }

    public FindUsagesWindowTreeState(AssetFindUsagesResultBase[] requests)
    {
      foreach (var request in requests)
      {
          switch (request)
          {
              case HierarchyFindUsagesResult hierarchyFindUsagesResult when request.FilePath.EndsWith(".prefab"):
                  PrefabElements.Add(new PrefabElement(request.FilePath, request.FileName, hierarchyFindUsagesResult.PathElements,
                      hierarchyFindUsagesResult.RootIndices));
                  break;
              case HierarchyFindUsagesResult hierarchyFindUsagesResult:
                  SceneElements.Add(new SceneElement(request.FilePath, request.FileName, hierarchyFindUsagesResult.PathElements,
                      hierarchyFindUsagesResult.RootIndices));
                  break;
              case AnimatorFindUsagesResult animatorUsage:
                  AnimatorElements.Add(new AnimatorElement(animatorUsage.Type, animatorUsage.FilePath,
                      animatorUsage.FileName, animatorUsage.PathElements, EmptyArray<int>.Instance));
                  break;
              case AnimationFindUsagesResult animationEventUsage:
                  AnimationElements.Add(new AnimationElement(
                      animationEventUsage.FilePath, animationEventUsage.FileName, EmptyArray<string>.Instance,
                      EmptyArray<int>.Instance));
                  break;
              default:
                  ScriptableObjectElements.Add(new ScriptableObjectElement(request.FilePath, request.FileName, EmptyArray<string>.Instance, EmptyArray<int>.Instance));
                  break;
          }
      }
    }
  }
}