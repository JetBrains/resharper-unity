using JetBrains.Diagnostics;
using JetBrains.Diagnostics.Internal;
using JetBrains.Lifetimes;

namespace JetBrains.Rider.Unity.Editor.Logger
{
  internal static class LogInitializer
  {
    private static SequentialLifetimes ourLifetimes;

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
        var fileLogFactory = Log.CreateFileLogFactory(lifetime, PluginEntryPoint.LogPath, true, selectedLoggingLevel);
        fileLogFactory.Handlers += message =>
        {
          if (lifetime.IsAlive && (message.Level == LoggingLevel.ERROR || message.Level == LoggingLevel.FATAL))
            MainThreadDispatcher.Instance.Queue(() =>
            {
              if (lifetime.IsAlive)
                UnityEngine.Debug.LogError(message.FormattedMessage);
            });
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