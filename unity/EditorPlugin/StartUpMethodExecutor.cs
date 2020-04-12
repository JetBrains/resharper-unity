using JetBrains.Annotations;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor
{
  // Do not rename this class while you don't rename startup command for dotTrace profiler
  [UsedImplicitly]
  public static class StartUpMethodExecutor
  {
    public static void EnterPlayMode()
    {
      EditorApplication.isPlaying = true;
    }
  }
}