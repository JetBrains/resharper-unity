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
    [SerializeField]
    public readonly int[] RootIndices;

    protected AbstractUsageElement(string filePath, string[] path, int[] rootIndices)
    {
      FilePath = filePath;
      Path = path;
      RootIndices = rootIndices;
    }

    public abstract string StartNodeImage { get; }
    public abstract string NodeImage { get; }
    public abstract string TerminalNodeImage { get; }
  }
}