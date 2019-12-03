using JetBrains.Collections.Viewable;
using JetBrains.Diagnostics;
using JetBrains.Rd.Tasks;
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

      CompiledAssembliesTracker.Init(modelAndLifetime);
      
      modelValue.UnitTestLaunch.Advise(connectionLifetime, launch =>
      {
        new TestEventsSender(launch);
      });
      
      modelValue.RunUnitTestLaunch.Set(rdVoid =>
      {
        if (!modelValue.UnitTestLaunch.HasValue()) return false;
        var testLauncher = new UnityEditorTestLauncher(modelValue.UnitTestLaunch.Value, connectionLifetime);
        return testLauncher.TryLaunchUnitTests();
      });
    }
  }
}