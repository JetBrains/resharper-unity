using System.Diagnostics;
using System.IO;
using JetBrains.Diagnostics;
using JetBrains.Diagnostics.Internal;
using JetBrains.Lifetimes;

namespace JetBrains.Rider.Unity.Editor.Logger
{
    public static class LogInitializer
    {
        public static void InitLog(LoggingLevel selectedLoggingLevel)
        {
            if (selectedLoggingLevel > LoggingLevel.OFF)
            {
                var fileLogFactory = Log.CreateFileLogFactory(Lifetime.Eternal, PluginEntryPoint.LogPath, true, selectedLoggingLevel);
                fileLogFactory.Handlers += message =>
                {
                    if (message.Level == LoggingLevel.ERROR || message.Level == LoggingLevel.FATAL)
                        MainThreadDispatcher.Instance.Queue(() => UnityEngine.Debug.LogError(message.FormattedMessage));
                };
                Log.DefaultFactory = fileLogFactory;
            }
            else
                Log.DefaultFactory = new SingletonLogFactory(NullLog.Instance); // use profiler in Unity - this is faster than leaving TextWriterLogFactory with LoggingLevel OFF
        }
    }
}