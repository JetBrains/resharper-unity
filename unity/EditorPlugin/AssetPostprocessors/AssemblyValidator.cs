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

    private static ushort ReadUInt16(this Stream stream)
    {
      return (ushort) (stream.ReadByte() | stream.ReadByte() << 8);
    }

    private static uint ReadUInt32(this Stream stream)
    {
      return (uint) (stream.ReadByte() | stream.ReadByte() << 8 | stream.ReadByte() << 16 | stream.ReadByte() << 24);
    }

    public static bool IsAssembly(string file)
    {
      using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
      {
        if (!fs.CanRead)
          throw new ArgumentException("Cannot read from stream.");
        if (!fs.CanSeek)
          throw new ArgumentException("Cannot seek in stream.");

        if (fs.Length < 318L || fs.ReadUInt16() != 23117 ||
            (!fs.Advance(58) || !fs.MoveTo(fs.ReadUInt32())) ||
            (fs.ReadUInt32() != 17744U || !fs.Advance(20)) ||
            !fs.Advance(fs.ReadUInt16() == (ushort) 523 ? 222 : 206))
          return false;
        return fs.ReadUInt32() > 0U;
      }
    }
  }
}