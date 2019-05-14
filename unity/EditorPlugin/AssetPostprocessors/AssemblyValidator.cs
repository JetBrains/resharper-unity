using System;
using System.IO;

namespace JetBrains.Rider.Unity.Editor.AssetPostprocessors
{
  public static class AssemblyValidator
  {
    private static bool Advance(this Stream stream, int length)
    {
      if (stream.Position + length >= stream.Length)
        return false;
      stream.Seek(length, SeekOrigin.Current);
      return true;
    }

    private static bool MoveTo(this Stream stream, uint pos)
    {
      if (pos >= stream.Length)
        return false;
      stream.Position = pos;
      return true;
    }

    public static bool IsAssembly(string file)
    {
      using (var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
      {
        if (!fileStream.CanRead)
          throw new ArgumentException("Cannot read from stream.");

        if (!fileStream.CanSeek)
          throw new ArgumentException("Cannot seek in stream.");

        using (var binaryReader = new BinaryReader(fileStream))
        {
          if (fileStream.Length < 318L || binaryReader.ReadUInt16() != 23117 ||
              (!fileStream.Advance(58) || !fileStream.MoveTo(binaryReader.ReadUInt32())) ||
              (binaryReader.ReadUInt32() != 17744U || !fileStream.Advance(20)) ||
              !fileStream.Advance(binaryReader.ReadUInt16() == (ushort) 523 ? 222 : 206))
            return false;
          return binaryReader.ReadUInt32() > 0U;
        }
      }
    }
  }
}