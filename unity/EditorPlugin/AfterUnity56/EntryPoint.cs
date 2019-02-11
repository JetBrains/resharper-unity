using System;
using JetBrains.Diagnostics;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.AfterUnity56
{
  [InitializeOnLoad]
  public static class EntryPoint
  {
    private static readonly ILog ourLogger = Log.GetLog("AfterUnity56.EntryPoint");
    
    static EntryPoint()
    {
      if (!PluginEntryPoint.Enabled)
        return;
      ourLogger.Verbose("AfterUnity56.EntryPoint");

      PluginEntryPoint.OnModelInitialization += UnitTesting.Initialization.OnModelInitializationHandler;
      PluginEntryPoint.OnModelInitialization += Navigation.Initialization.OnModelInitializationHandler;
      AppDomain.CurrentDomain.DomainUnload += (EventHandler) ((_, __) =>
      {
        PluginEntryPoint.OnModelInitialization -= UnitTesting.Initialization.OnModelInitializationHandler;
        PluginEntryPoint.OnModelInitialization -= Navigation.Initialization.OnModelInitializationHandler;
      });
    }
  }
}