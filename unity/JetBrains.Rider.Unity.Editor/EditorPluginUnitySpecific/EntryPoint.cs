using JetBrains.Rider.Unity.Editor.Ge56.UnitTesting;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor.Ge56
{
  [InitializeOnLoad]
  public static class EntryPoint
  {
    static EntryPoint()
    {
      PluginEntryPoint.ModelCallbacksList.Add(ModelAdviceExtension.AdviseUnitTestLaunch);
    }
  }
}