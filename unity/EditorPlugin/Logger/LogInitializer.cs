using System;
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
                var lifetimeDefinition = Lifetime.Eternal.CreateNested();
                var lifetime = lifetimeDefinition.Lifetime;
                AppDomain.CurrentDomain.DomainUnload += (sender, args) =>
                {
                    lifetimeDefinition.Terminate();
                };
                
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
                Log.DefaultFactory = new SingletonLogFactory(NullLog.Instance); // use profiler in Unity - this is faster than leaving TextWriterLogFactory with LoggingLevel OFF
        }
    }
}