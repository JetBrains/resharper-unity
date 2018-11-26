using System;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.Navigation
{
  [Serializable]
  internal class PrefabElement : AbstractUsageElement
  {
    public PrefabElement(string filePath, string[] path, int[] rootIndices) : base(filePath, path, rootIndices)
    {
    }

    public override string StartNodeImage => "Prefab Icon";
    public override string NodeImage => "GameObject Icon";
    public override string TerminalNodeImage => "GameObject Icon";
  }
}