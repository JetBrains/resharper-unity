using System.IO;
using UnityEditor;

namespace JetBrains.Rider.Unity.Editor
{
  public static class EditorPrefsWrapper
  {
    public static string ExternalScriptEditor
    {
      get { return EditorPrefs.GetString("kScriptsDefaultApp"); }
      set { EditorPrefs.SetString("kScriptsDefaultApp", value); }
    }

    public static bool AutoRefresh
    {
      get { return EditorPrefs.GetBool("kAutoRefresh"); }
      set { EditorPrefs.SetBool("kAutoRefresh", value); }
    }
    
    public static int ScriptChangesDuringPlayOptions
    {
      // https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/PreferencesWindow/PreferencesSettingsProviders.cs#L1180
      get { return EditorPrefs.GetInt("ScriptCompilationDuringPlay", 0); }
      set { EditorPrefs.SetInt("ScriptCompilationDuringPlay", value); }
    }
  }
}