using System;

namespace JetBrains.Rider.Unity.Editor.Navigation.Window
{
  
  [Serializable]
  public class ScriptableObjectElement : AbstractUsageElement
  {
    public ScriptableObjectElement(string filePath, string fileName, string[] path, int[] rootIndices)
      : base(filePath, fileName, path, rootIndices)
    {
    }

    public override string StartNodeImage => "ScriptableObject Icon";
    public override string NodeImage => "ScriptableObject Icon";
    public override string TerminalNodeImage => NodeImage;
  }
}