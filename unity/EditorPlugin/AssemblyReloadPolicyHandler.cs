using JetBrains.DataFlow;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor
{
  public static class AssemblyReloadPolicyHandler
  {
    private enum PlayModeState
    {
      Stopped,
      Playing,
      Paused
    }

    private static PlayModeState ourSavedState = PlayModeState.Stopped;

    public static void Initialise(Lifetime appDomainLifetime)
    {
      ourSavedState = GetEditorState();

      PreventAssemblyReloadWhenPlaying();
      StopPlayingOnAssemblyReload(appDomainLifetime);
    }

    private static PlayModeState GetEditorState()
    {
      if (EditorApplication.isPaused)
        return PlayModeState.Paused;
      if (EditorApplication.isPlaying)
        return PlayModeState.Playing;
      return PlayModeState.Stopped;
    }

    private static void PreventAssemblyReloadWhenPlaying()
    {
#pragma warning disable 618
      EditorApplication.playmodeStateChanged += () =>
#pragma warning restore 618
      {
        var newState = GetEditorState();
        if (ourSavedState != newState)
        {
          if (PluginSettings.AssemblyReloadSettings == AssemblyReloadSettings.RecompileAfterFinishedPlaying)
          {
            if (newState == PlayModeState.Playing)
            {
              EditorApplication.LockReloadAssemblies();
            }
            else if (newState == PlayModeState.Stopped)
            {
              EditorApplication.UnlockReloadAssemblies();
            }
          }
          ourSavedState = newState;
        }
      };
    }

    private static void StopPlayingOnAssemblyReload(Lifetime appDomainLifetime)
    {
      appDomainLifetime.AddAction(() =>
      {
        if (PluginSettings.AssemblyReloadSettings == AssemblyReloadSettings.StopPlayingAndRecompile)
        {
          if (EditorApplication.isPlaying)
          {
            EditorApplication.isPlaying = false;
          }
        }
      });
    }
  }
}