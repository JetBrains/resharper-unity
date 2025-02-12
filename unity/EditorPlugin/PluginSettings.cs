using System;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Diagnostics;
using JetBrains.Rider.Model.Unity;
using JetBrains.Rider.PathLocator;
using UnityEditor;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor
{
  internal interface IPluginSettings
  {
    OS OSRider { get; }
  }

  internal class PluginSettings : IPluginSettings
  {
    public static LoggingLevel SelectedLoggingLevel
    {
      get => (LoggingLevel) EditorPrefs.GetInt("Rider_SelectedLoggingLevel", 0);
      set
      {
        EditorPrefs.SetInt("Rider_SelectedLoggingLevel", (int) value);
        LogInitializer.SetLogLevel(value);
      }
    }

    public static bool LogEventsCollectorEnabled
    {
      get { return EditorPrefs.GetBool("Rider_LogEventsCollectorEnabled", true); }
      private set { EditorPrefs.SetBool("Rider_LogEventsCollectorEnabled", value); }
    }

    public OS OSRider => SystemInfoRiderPlugin.OS;

    internal static class SystemInfoRiderPlugin
    {
      // This call on Linux is extremely slow, so cache it
      private static readonly string ourOperatingSystem = SystemInfo.operatingSystem;

      // Do not rename. Explicitly disabled for consistency/compatibility with future Unity API
      // ReSharper disable once InconsistentNaming
      public static OS OS
      {
        get
        {
          if (ourOperatingSystem.StartsWith("Mac", StringComparison.InvariantCultureIgnoreCase))
          {
            return OS.MacOSX;
          }

          if (ourOperatingSystem.StartsWith("Win", StringComparison.InvariantCultureIgnoreCase))
          {
            return OS.Windows;
          }

          if (ourOperatingSystem.StartsWith("Lin", StringComparison.InvariantCultureIgnoreCase))
          {
            return OS.Linux;
          }

          return OS.Other;
        }
      }
    }
  }
}