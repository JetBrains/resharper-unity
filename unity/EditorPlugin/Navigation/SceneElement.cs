using System;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.Navigation
{
  [Serializable]
  internal class SceneElement : AbstractUsageElement
  {
    [SerializeField] 
    public string LocalId;

    public SceneElement(string scenePath, string[] path, string localId) : base(scenePath, path)
    {
      LocalId = localId;
    }

    public override string StartNodeImage => "SceneAsset Icon";
    public override string NodeImage => "GameObject Icon";
    public override string TerminalNodeImage => NodeImage;
  }
}