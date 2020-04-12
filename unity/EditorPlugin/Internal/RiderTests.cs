using JetBrains.Annotations;
using JetBrains.Diagnostics;

namespace JetBrains.Rider.Unity.Editor.Internal
{
  [UsedImplicitly]
  public static class RiderTests
  {
    [UsedImplicitly]
    public static void  EnableLogsSyncSolution()
    {
      PluginSettings.SelectedLoggingLevel = LoggingLevel.TRACE;
      UnityUtils.SyncSolution();
    }
  }
}