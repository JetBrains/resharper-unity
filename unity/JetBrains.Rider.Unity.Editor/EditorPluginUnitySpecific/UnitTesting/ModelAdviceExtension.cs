using JetBrains.DataFlow;
using JetBrains.Platform.Unity.Model;

namespace JetBrains.Rider.Unity.Editor.Ge56.UnitTesting
{
  public static class ModelAdviceExtension
  {
    public static void AdviseUnitTestLaunch(UnityModel modelValue, Lifetime connectionLifetime)
    {     
      modelValue.UnitTestLaunch.Change.Advise(connectionLifetime, launch =>
      {
        var unityEditorTestLauncher = new UnityEditorTestLauncher(launch);
        unityEditorTestLauncher.TryLaunchUnitTests();
      });
    }
  }
}