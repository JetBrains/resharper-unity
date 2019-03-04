using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.AfterUnity56.UnitTesting
{
  public static class Initialization
  {
    private static readonly ILog ourLogger = Log.GetLog("UnitTesting.Initialization");
    
    public static void OnModelInitializationHandler(UnityModelAndLifetime modelAndLifetime)
    {
      ourLogger.Verbose("AdviseUnitTestLaunch");
      var modelValue = modelAndLifetime.Model;
      var connectionLifetime = modelAndLifetime.Lifetime;
      
      modelValue.GetCompilationResult.Set(_ => !EditorUtility.scriptCompilationFailed);
      
      modelValue.UnitTestLaunch.Advise(connectionLifetime, launch =>
      {
        var collector = TestEventsCollector.Instance;
        ourLogger.Verbose("TestEventsCollectorInstance: " + collector.GetInstanceID()+" DelayedEvents.Count:"+ collector.DelayedEvents.Count);
        new TestEventsSender(collector, launch);
      });
      
      modelValue.RunUnitTestLaunch.Advise(connectionLifetime, () =>
      {
        if (!modelValue.UnitTestLaunch.HasValue()) return;
        var testLauncher = new UnityEditorTestLauncher(modelValue.UnitTestLaunch.Value);
        testLauncher.TryLaunchUnitTests();
      });
    }
  }
}