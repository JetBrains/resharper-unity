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
      EditorApplication.playModeStateChanged += _ => Current.Value = GetPlayModeState();
      EditorApplication.pauseStateChanged += _ => Current.Value = GetPlayModeState();
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
