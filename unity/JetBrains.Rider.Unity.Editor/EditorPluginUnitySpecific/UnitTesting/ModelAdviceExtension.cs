namespace JetBrains.Rider.Unity.Editor.Ge56.UnitTesting
{
  public static class ModelAdviceExtension
  {
    public static void AdviseUnitTestLaunch(UnityModelAndLifetime modelAndLifetime)
    {
      var modelValue = modelAndLifetime.Model;
      var connectionLifetime = modelAndLifetime.Lifetime;
      modelValue.UnitTestLaunch.Change.Advise(connectionLifetime, launch =>
      {
        var unityEditorTestLauncher = new UnityEditorTestLauncher(launch);
        unityEditorTestLauncher.TryLaunchUnitTests();
      });
    }
  }
}