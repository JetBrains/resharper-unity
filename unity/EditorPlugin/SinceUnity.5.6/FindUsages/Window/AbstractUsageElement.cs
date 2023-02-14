using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.FindUsages.Window
{
  [Serializable]
  [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
  [SuppressMessage("ReSharper", "InconsistentNaming")]
  public abstract class AbstractUsageElement
  {
    [SerializeField]
    public string FilePath;
    [SerializeField]
    public string FileName;
    [SerializeField]
    public string[] Path;
    [SerializeField]
    public int[] RootIndices;

    protected AbstractUsageElement(string filePath, string fileName, string[] path, int[] rootIndices)
    {
      FilePath = filePath;
      FileName = fileName;
      Path = path;
      RootIndices = rootIndices;
    }

    public abstract string StartNodeImage { get; }
    public abstract string NodeImage { get; }
    public abstract string TerminalNodeImage { get; }
  }
}