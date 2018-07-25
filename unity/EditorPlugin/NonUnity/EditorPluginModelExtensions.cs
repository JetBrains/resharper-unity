using System;
using JetBrains.Platform.RdFramework;
using JetBrains.Platform.RdFramework.Tasks;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.Util.Logging;

namespace JetBrains.Rider.Unity.Editor.NonUnity
{
  public static class EditorPluginModelExtensions
  {
    private static readonly ILog ourLogger = Log.GetLog("EditorPluginModel");

    internal static bool CheckConnectedToBackendSync(this EditorPluginModel model)
    {
      if (model == null)
        return false;

      var connected = false;
      try
      {
        // HostConnected also means that in Rider and in Unity the same solution is opened
        connected = model.IsBackendConnected.Sync(RdVoid.Instance,
          new RpcTimeouts(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(200)));
      }
      catch (Exception)
      {
        ourLogger.Verbose("Rider Protocol not connected.");
      }

      return connected;
    }
  }
}