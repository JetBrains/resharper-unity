using System;

namespace JetBrains.Rider.Unity.Editor.Navigation.Window
{
  [Serializable]
  internal class SceneElement : AbstractUsageElement
  {
    public SceneElement(string scenePath, string fileName, string[] path, int[] rootIndices) : base(scenePath, fileName, path, rootIndices)
    {
    }

    public override string StartNodeImage => "SceneAsset Icon";
    public override string NodeImage => "GameObject Icon";
    public override string TerminalNodeImage => NodeImage;
  }
}