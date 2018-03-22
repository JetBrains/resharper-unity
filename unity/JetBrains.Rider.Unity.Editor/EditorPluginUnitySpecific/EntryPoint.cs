using System;
using JetBrains.Rider.Unity.Editor.Ge56.UnitTesting;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.Ge56
{
  [InitializeOnLoad]
  public static class EntryPoint
  {
    static EntryPoint()
    {
      PluginEntryPoint.OnModelInitialization+=ModelAdviceExtension.AdviseUnitTestLaunch;
      AppDomain.CurrentDomain.DomainUnload += (EventHandler) ((_, __) =>
      {
        PluginEntryPoint.OnModelInitialization-=ModelAdviceExtension.AdviseUnitTestLaunch;
      });
    }
  }
}