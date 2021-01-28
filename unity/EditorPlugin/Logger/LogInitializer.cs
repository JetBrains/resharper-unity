using System.Diagnostics;
using System.IO;
using JetBrains.Diagnostics;
using JetBrains.Diagnostics.Internal;
using JetBrains.Lifetimes;

namespace JetBrains.Rider.Unity.Editor.Logger
{
    public static class LogInitializer
    {
        private static readonly string ourBaseLogPath = !UnityUtils.IsInRiderTests
            ? Path.GetTempPath()
            : new FileInfo(UnityUtils.UnityEditorLogPath).Directory.FullName;

        internal static readonly string LogPath = Path.Combine(Path.Combine(ourBaseLogPath, "Unity3dRider"), $"EditorPlugin.{Process.GetCurrentProcess().Id}.log");
        public static void InitLog(LoggingLevel selectedLoggingLevel)
        {
            if (selectedLoggingLevel > LoggingLevel.OFF)
                Log.DefaultFactory = Log.CreateFileLogFactory(Lifetime.Eternal, LogPath, true, selectedLoggingLevel);
            else
                Log.DefaultFactory = new SingletonLogFactory(NullLog.Instance); // use profiler in Unity - this is faster than leaving TextWriterLogFactory with LoggingLevel OFF
        }
    }
}