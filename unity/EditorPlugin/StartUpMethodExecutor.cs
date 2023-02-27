using JetBrains.Annotations;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor
{
  // DO NOT CHANGE NAME OR NAMESPACE!
  // Used as startup args when invoking the "Attach to Unity Editor and Play" run configuration for profiling, and the
  // editor isn't currently running.
  // This class is duplicated in the package and the plugin, with the same name and namespace. If the plugin is loaded
  // from Assets, then the class is available in the editor. If not, then it is available via the package
  [PublicAPI]
  public static class StartUpMethodExecutor
  {
    [PublicAPI]
    public static void EnterPlayMode()
    {
      EditorApplication.isPlaying = true;
    }
  }
}