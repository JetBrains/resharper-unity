using System;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.Navigation
{
  [Serializable]
  internal abstract class AbstractUsageElement
  {
    [SerializeField] 
    public string FilePath;
    [SerializeField]
    public string[] Path;

    protected AbstractUsageElement(string filePath, string[] path)
    {
      FilePath = filePath;
      Path = path;
    }

    public abstract string StartNodeImage { get; }
    public abstract string NodeImage { get; }
    public abstract string TerminalNodeImage { get; }
  }
}