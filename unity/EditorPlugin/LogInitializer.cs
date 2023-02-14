using System.Diagnostics;
using System.IO;
using JetBrains.Diagnostics;
using JetBrains.Diagnostics.Internal;
using JetBrains.Lifetimes;

namespace JetBrains.Rider.Unity.Editor
{
  internal static class LogInitializer
  {
    private static SequentialLifetimes ourLifetimes;

    internal static readonly string LogPath;

    static LogInitializer()
    {
      var baseLogPath = UnityUtils.IsInRiderTests
        ? new FileInfo(UnityUtils.UnityEditorLogPath).Directory.NotNull().FullName
        : Path.GetTempPath();
      LogPath = Path.Combine(Path.Combine(baseLogPath, "Unity3dRider"),
        $"EditorPlugin.{Process.GetCurrentProcess().Id}.log");
    }

    public static void InitLog(Lifetime lifetime, LoggingLevel selectedLoggingLevel)
    {
      ourLifetimes = new SequentialLifetimes(lifetime);
      SetLogLevel(selectedLoggingLevel);
    }

    public static void SetLogLevel(LoggingLevel selectedLoggingLevel)
    {
      if (selectedLoggingLevel > LoggingLevel.OFF)
      {
        var lifetime = ourLifetimes.Next();
        var fileLogFactory = Log.CreateFileLogFactory(lifetime, LogPath, true, selectedLoggingLevel);
        fileLogFactory.Handlers += message =>
        {
          if (lifetime.IsAlive && (message.Level == LoggingLevel.ERROR || message.Level == LoggingLevel.FATAL))
          {
            // We should only be called on the main thread
            MainThreadDispatcher.Instance.Queue(() =>
            {
              if (lifetime.IsAlive)
                UnityEngine.Debug.LogError(message.FormattedMessage);
            });
          }
        };
        Log.DefaultFactory = fileLogFactory;
      }
      else
      {
        ourLifetimes.TerminateCurrent();
        // Use profiler in Unity - this is faster than leaving TextWriterLogFactory with LoggingLevel OFF
        Log.DefaultFactory = new SingletonLogFactory(NullLog.Instance);
      }
    }
  }
}