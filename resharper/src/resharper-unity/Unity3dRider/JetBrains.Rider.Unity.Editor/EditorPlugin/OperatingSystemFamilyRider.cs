using System;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor
{
  public enum OperatingSystemFamilyRider
  {
    Other,
    // ReSharper disable once InconsistentNaming
    MacOSX,
    Windows,
    Linux,
  }

  public static class SystemInfoRiderPlugin
  {
    // ReSharper disable once InconsistentNaming
    public static OperatingSystemFamilyRider operatingSystemFamily
    {
      get
      {
        if (SystemInfo.operatingSystem.StartsWith("Mac", StringComparison.InvariantCultureIgnoreCase))
        {
          return OperatingSystemFamilyRider.MacOSX;
        }
        if (SystemInfo.operatingSystem.StartsWith("Win", StringComparison.InvariantCultureIgnoreCase))
        {
          return OperatingSystemFamilyRider.Windows;
        }
        if (SystemInfo.operatingSystem.StartsWith("Lin", StringComparison.InvariantCultureIgnoreCase))
        {
          return OperatingSystemFamilyRider.Linux;
        }
        return OperatingSystemFamilyRider.Other;
      }
    }
  }

}