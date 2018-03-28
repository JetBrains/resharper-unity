using System;
using System.IO;
using System.Threading;
using JetBrains.Util;
using JetBrains.Util.Logging;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor
{
  public class RiderLoggerFactory : ILogFactory
  {
    public ILog GetLog(string category)
    {
      return new RiderLogger(category);
    }
  }

  public class RiderLogger : ILog
  {
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
        if (!new FileInfo(PluginEntryPoint.LogPath).Directory.Exists)
          new FileInfo(PluginEntryPoint.LogPath).Directory.Create();
        File.AppendAllText(PluginEntryPoint.LogPath, Environment.NewLine + text);
      });
    }

    public string Category { get; }
  }
}
