using System;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.Navigation
{
  [Serializable]
  internal class SceneElement : AbstractUsageElement
  {
    public SceneElement(string scenePath, string[] path, int[] rootIndices) : base(scenePath, path, rootIndices)
    {
    }

    public override string StartNodeImage => "SceneAsset Icon";
    public override string NodeImage => "GameObject Icon";
    public override string TerminalNodeImage => NodeImage;
  }
}