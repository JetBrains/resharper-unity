using System;
using JetBrains.Util.Logging;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.UnitTesting
{
  [InitializeOnLoad]
  public static class UnitTestingEntryPoint
  {
    private static readonly ILog ourLogger = Log.GetLog("UnitTestingEntryPoint");

    static UnitTestingEntryPoint()
    {
      // Ordering of InitializeOnLoad methods is undefined
      RiderLoggerFactory.Init();

      ourLogger.Verbose("UnitTesting.EntryPoint");

      PluginEntryPoint.OnModelInitialization += ModelAdviceExtension.AdviseUnitTestLaunch;
      AppDomain.CurrentDomain.DomainUnload += (EventHandler) ((_, __) =>
      {
        PluginEntryPoint.OnModelInitialization -= ModelAdviceExtension.AdviseUnitTestLaunch;
      });
    }
  }
}