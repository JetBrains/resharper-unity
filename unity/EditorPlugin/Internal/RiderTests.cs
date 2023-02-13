using JetBrains.Annotations;
using JetBrains.Diagnostics;

namespace JetBrains.Rider.Unity.Editor.Internal
{
  // TODO: Confirm if this is still used. There are no text usages in the resharper-unity repo
  [UsedImplicitly]
  public static class RiderTests
  {
    [UsedImplicitly]
    public static void EnableLogsSyncSolution()
    {
      PluginSettings.SelectedLoggingLevel = LoggingLevel.TRACE;
      UnityUtils.SyncSolution();
    }
  }
}