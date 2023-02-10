using System;

namespace JetBrains.Rider.Unity.Editor.Navigation.Window
{
  [Serializable]
  internal class PrefabElement : AbstractUsageElement
  {
    public PrefabElement(string filePath, string fileName, string[] path, int[] rootIndices) : base(filePath, fileName, path, rootIndices)
    {
    }

    public override string StartNodeImage => "Prefab Icon";
    public override string NodeImage => "GameObject Icon";
    public override string TerminalNodeImage => "GameObject Icon";
  }
}