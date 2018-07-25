using System;
using System.IO;
using JetBrains.Util;
using JetBrains.Util.Logging;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor
{
  public class RiderLoggerFactory : ILogFactory
  {
    public static void Init()
    {
      Log.DefaultFactory = new RiderLoggerFactory();
    }

    public ILog GetLog(string category)
    {
      return new RiderLogger(category);
    }
  }

  public class RiderLogger : ILog
  {
    internal static readonly string LogPath = Path.Combine(Path.Combine(Path.GetTempPath(), "Unity3dRider"), DateTime.Now.ToString("yyyy-MM-ddT-HH-mm-ss") + ".log");

    public RiderLogger(string category)
    {
      Category = category;
    }

    public bool IsEnabled(LoggingLevel level)
    {
      var levelFromSettings = PluginSettings.SelectedLoggingLevel;
      return level <= levelFromSettings;
    }

    public void Log(LoggingLevel level, string message, Exception exception = null)
    {
      if (!IsEnabled(level))
        return;

      // ReSharper disable once StringLastIndexOfIsCultureSpecific.1
      var dotidx = Category.LastIndexOf(".");
      var categoryText = Category.Substring(dotidx >= 0 ? dotidx + 1 : 0);

      var dateTime = "";
      try
      {
        // Unity may crash on this
        dateTime = DateTime.Now.ToString(Util.Logging.Log.DefaultDateFormat);
      }
      catch (Exception e)
      {
        Debug.Log("DateTime.Now: "+ DateTime.Now);
        Debug.LogError(e);
      }

      var text = categoryText + "[" + level + "]" + dateTime + " " + message;
      if (exception != null)
        text = text + Environment.NewLine + exception.Message + Environment.NewLine + exception.StackTrace;

      // using Unity logs causes frequent Unity hangs
      MainThreadDispatcher.Instance.Queue(() =>
      {
        var directory = new FileInfo(LogPath).Directory;
        if (!directory.Exists)
          directory.Create();
        File.AppendAllText(LogPath, Environment.NewLine + text);
      });
    }

    public string Category { get; }
  }
}
