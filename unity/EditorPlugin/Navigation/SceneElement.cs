using System;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.Navigation
{
  [Serializable]
  internal class SceneElement
  {
    [SerializeField] 
    public string SceneName;
    [SerializeField]
    public string[] Path;
    [SerializeField] 
    public int LocalId;

    public SceneElement(string sceneName, string[] path, int localId)
    {
      SceneName = sceneName;
      Path = path;
      LocalId = localId;
    }
  }
}