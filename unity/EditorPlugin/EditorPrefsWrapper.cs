using JetBrains.Rider.Model.Unity;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor
{
  public static class EditorPrefsWrapper
  {
    public static string ExternalScriptEditor
    {
      get => EditorPrefs.GetString("kScriptsDefaultApp");
      set => EditorPrefs.SetString("kScriptsDefaultApp", value);
    }

    public static bool AutoRefresh
    {
      get => EditorPrefs.GetBool("kAutoRefresh");
      set => EditorPrefs.SetBool("kAutoRefresh", value);
    }

    // This is an internal Unity setting. Introduced in 2018.2 (moved in 2018.3). The enum is internal, so we can only
    // use the int value, which matches ScriptCompilationDuringPlay, which is a protocol type, and we can't rely on the
    // int mapping.
    // https://github.com/Unity-Technologies/UnityCsReference/blob/2018.2/Editor/Mono/PreferencesWindow/PreferencesWindow.cs#L1154
    // https://github.com/Unity-Technologies/UnityCsReference/blob/2018.3/Editor/Mono/PreferencesWindow/PreferencesSettingsProviders.cs#L1074
    public static ScriptCompilationDuringPlay ScriptCompilationDuringPlay
    {
      get => UnityUtils.ToScriptCompilationDuringPlay(EditorPrefs.GetInt("ScriptCompilationDuringPlay",
              UnityUtils.FromScriptCompilationDuringPlay(ScriptCompilationDuringPlay.RecompileAndContinuePlaying)));
      set => EditorPrefs.SetInt("ScriptCompilationDuringPlay", UnityUtils.FromScriptCompilationDuringPlay(value));
    }
  }
}