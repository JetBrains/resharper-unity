using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Platform.RdFramework.Base;
using JetBrains.Platform.RdFramework.Util;
using JetBrains.Platform.Unity.EditorPluginModel;
using JetBrains.Util.Logging;
using UnityEditor;
using UnityEngine;

namespace JetBrains.Rider.Unity.Editor.UnitTesting
{
  [InitializeOnLoad]
  public static class EntryPoint
  {
    private static readonly ILog ourLogger = Log.GetLog("UnitTesting.EntryPoint");
    private static object myPlayModeTestsController;
    //private static readonly TestEventsCollector ourCollector;

    static EntryPoint()
    {
      if (!PluginEntryPoint.Enabled)
        return;
      ourLogger.Verbose("UnitTesting.EntryPoint");

      PluginEntryPoint.OnModelInitialization += OnModelInitializationHandler;
      AppDomain.CurrentDomain.DomainUnload += (EventHandler) ((_, __) =>
      {
        PluginEntryPoint.OnModelInitialization -= OnModelInitializationHandler;
      });
    }

    private static void OnModelInitializationHandler(UnityModelAndLifetime modelAndLifetime)
    {
      ourLogger.Verbose("AdviseUnitTestLaunch");
      var modelValue = modelAndLifetime.Model;
      var connectionLifetime = modelAndLifetime.Lifetime;
      
      modelValue.UnitTestLaunch.Advise(connectionLifetime, launch =>
      {
        var collector = ScriptableObject.FindObjectOfType<TestEventsCollector>();
        if (collector != null)
          new TestEventsSender(collector, launch);
      });
      
      modelValue.RunUnitTestLaunch.Advise(connectionLifetime, () =>
      {
        if (!modelValue.UnitTestLaunch.HasValue())
          return;
        
        var unityEditorTestLauncher = new UnityEditorTestLauncher(modelValue.UnitTestLaunch.Value);
        unityEditorTestLauncher.TryLaunchUnitTests();
      });
    }
  }
}