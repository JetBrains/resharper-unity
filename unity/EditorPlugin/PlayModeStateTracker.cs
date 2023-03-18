using JetBrains.Collections.Viewable;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor
{
  internal enum PlayModeState
  {
    Stopped,
    Playing,
    Paused
  }

  // Abstraction to hide the complexity of playmodeStateChanged vs playModeStateChanged, and to also cache the play mode
  // so we don't make a native engine call for each log message we send to Rider
  internal static class PlayModeStateTracker
  {
    public static readonly IViewableProperty<PlayModeState> Current = new ViewableProperty<PlayModeState>(GetPlayModeState());

    public static void Initialise()
    {
#if UNITY_2017_3_OR_NEWER
      EditorApplication.playModeStateChanged += _ => Current.Value = GetPlayModeState();
#else
      // playmodeStateChanged was marked obsolete in 2017.1, but it's still working in 2019.4
      EditorApplication.playmodeStateChanged += () => Current.Value = GetPlayModeState();
#endif
    }

    private static PlayModeState GetPlayModeState()
    {
      if (EditorApplication.isPaused)
        return PlayModeState.Paused;
      if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        return PlayModeState.Playing;
      return PlayModeState.Stopped;
    }
  }
}