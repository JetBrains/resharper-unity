using System;
using JetBrains.Diagnostics;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.AfterUnity56
{
  [InitializeOnLoad]
  public static class EntryPoint
  {
    static EntryPoint()
    {
      if (UnityUtils.IsInBatchModeAndNotInRiderTests)
        return;
      
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