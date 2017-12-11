using System;
using System.IO;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace Plugins.Editor.JetBrains
{
  public class RiderLogger : ILog
  {
    public bool IsEnabled(LoggingLevel level)
    {
      return level <= RiderPlugin.SelectedLoggingLevel;
    }

    public void Log(LoggingLevel level, string message, Exception exception = null)
    {
      if (!IsEnabled(level))
        return;

      var text = "[Rider][" + level + "]" + DateTime.Now.ToString("HH:mm:ss:ff") + " " + message;

      // using Unity logs causes frequent Unity hangs
      File.AppendAllText(RiderPlugin.logPath,Environment.NewLine + text);
//      switch (level)
//      {
//        case LoggingLevel.FATAL:
//        case LoggingLevel.ERROR:
//          Debug.LogError(text);
//          if (exception != null)
//            Debug.LogException(exception);
//          break;
//        case LoggingLevel.WARN:
//          Debug.LogWarning(text);
//          if (exception != null)
//            Debug.LogException(exception);
//          break;
//        case LoggingLevel.INFO:
//        case LoggingLevel.VERBOSE:
//          Debug.Log(text);
//          if (exception != null)
//            Debug.LogException(exception);
//          break;
//        default:
//          break;
//      }
    }

    public string Category { get; private set; }
  }
}