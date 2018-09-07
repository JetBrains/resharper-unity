using JetBrains.Util.Logging;

namespace JetBrains.Rider.Unity.Editor.UnitTesting
{
  public static class ModelAdviceExtension
  {
    private static readonly ILog ourLogger = Log.GetLog("ModelAdviceExtension");
    
    public static void AdviseUnitTestLaunch(UnityModelAndLifetime modelAndLifetime)
    {
      ourLogger.Verbose("AdviseUnitTestLaunch");
      var modelValue = modelAndLifetime.Model;
      var connectionLifetime = modelAndLifetime.Lifetime;
      modelValue.UnitTestLaunch.Change.Advise(connectionLifetime, launch =>
      {
        var unityEditorTestLauncher = new UnityEditorTestLauncher(launch, connectionLifetime);
        unityEditorTestLauncher.TryLaunchUnitTests();
      });
    }
  }
}