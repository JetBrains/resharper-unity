using System;
using System.Linq;
using JetBrains.Platform.RdFramework.Util;
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
    private static readonly TestEventsCollector ourCollector;

    static EntryPoint()
    {
      if (!PluginEntryPoint.Enabled)
        return;
      ourLogger.Verbose("EntryPoint");
      
      var assemblies = AppDomain.CurrentDomain.GetAssemblies();
      var testEngineAssembly = assemblies
        .FirstOrDefault(assembly => assembly.GetName().Name.Equals("UnityEngine.TestRunner"));
      
      var playModeTestsControllerTypeString = "UnityEngine.TestTools.TestRunner.PlaymodeTestsController";
      var playModeTestsControllerType = testEngineAssembly.GetType(playModeTestsControllerTypeString);
      var codeBasedTestsRunner = GameObject.Find("Code-based tests runner");
      myPlayModeTestsController =  codeBasedTestsRunner?.GetComponent(playModeTestsControllerType);
      ourCollector = new TestEventsCollector(myPlayModeTestsController);
      
      PluginEntryPoint.OnModelInitialization += AdviseUnitTestLaunch;
      AppDomain.CurrentDomain.DomainUnload += (EventHandler) ((_, __) =>
      {
        PluginEntryPoint.OnModelInitialization -= AdviseUnitTestLaunch;
      });
    }

    private static void AdviseUnitTestLaunch(UnityModelAndLifetime modelAndLifetime)
    {
      ourLogger.Verbose("AdviseUnitTestLaunch");
      var modelValue = modelAndLifetime.Model;
      var connectionLifetime = modelAndLifetime.Lifetime;
      modelValue.UnitTestLaunch.AdviseNotNull(connectionLifetime,
        launch => { new TestEventsSender(ourCollector, launch); });

      modelValue.UnitTestLaunch.Change.Advise(connectionLifetime, launch =>
      {
        var unityEditorTestLauncher = new UnityEditorTestLauncher(launch);
        unityEditorTestLauncher.TryLaunchUnitTests();
      });
    }
  }
}