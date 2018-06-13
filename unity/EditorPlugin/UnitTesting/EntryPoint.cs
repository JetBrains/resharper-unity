using System;
using JetBrains.Util.Logging;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.UnitTesting
{
  [InitializeOnLoad]
  public static class EntryPoint
  {
    private static readonly ILog ourLogger = Log.GetLog("UnitTesting.EntryPoint");
    
    static EntryPoint()
    {
      ourLogger.Verbose("EntryPoint");
      PluginEntryPoint.OnModelInitialization+=ModelAdviceExtension.AdviseUnitTestLaunch;
      AppDomain.CurrentDomain.DomainUnload += (EventHandler) ((_, __) =>
      {
        PluginEntryPoint.OnModelInitialization-=ModelAdviceExtension.AdviseUnitTestLaunch;
      });
    }
  }
}