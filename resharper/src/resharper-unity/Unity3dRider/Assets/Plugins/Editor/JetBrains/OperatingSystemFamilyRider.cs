using System;
using UnityEngine;

namespace Plugins.Editor.JetBrains
{
  public enum OperatingSystemFamilyRider
  {
    Other,
    MacOSX,
    Windows,
    Linux,
  }

  public static class SystemInfoRiderPlugin
  {
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