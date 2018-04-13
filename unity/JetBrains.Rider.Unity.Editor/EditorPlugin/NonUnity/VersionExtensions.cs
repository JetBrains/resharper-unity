using System;

namespace JetBrains.Rider.Unity.Editor.NonUnity
{
  public static class VersionExtensions
  {
    public static bool TryParse(string input, out Version version)
    {
      try
      {
        version = new Version(input);
        return true;
      }
      catch (ArgumentException)
      {
      } // can't put loggin here because ot fire on every symbol
      catch (FormatException)
      {
      }
      
      version = new Version(0,0);

      return false;
    }
  }
}