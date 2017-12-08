using System.IO;
using JetBrains.Util;
using UnityEditor;

namespace Plugins.Editor.JetBrains
{
  [InitializeOnLoad]
  public static partial class RiderPlugin1
  {
    static RiderPlugin1()
    {
      if (!Enabled) 
        return;
      // cache once on main thread
      SelectedLoggingLevel = SelectedLoggingLevelMainThread;
    }
    
    public static LoggingLevel SelectedLoggingLevel { get; private set; }

    public static LoggingLevel SelectedLoggingLevelMainThread
    {
      get { return (LoggingLevel) EditorPrefs.GetInt("Rider_SelectedLoggingLevel", 1); }
      set
      {
        SelectedLoggingLevel = value;
        EditorPrefs.SetInt("Rider_SelectedLoggingLevel", (int) value);
      }
    }
    
    public static bool SendConsoleToRider
    {
      get{return EditorPrefs.GetBool("Rider_SendConsoleToRider", false);}
      set{EditorPrefs.SetBool("Rider_SendConsoleToRider", value);}
    }
    
    public static bool Enabled
    {
      get
      {
        var defaultApp = GetExternalScriptEditor();
        return !string.IsNullOrEmpty(defaultApp) && Path.GetFileName(defaultApp).ToLower().Contains("rider");
      }
    }
    
    public static string GetExternalScriptEditor()
    {
      return EditorPrefs.GetString("kScriptsDefaultApp");
    }

    public static void SetExternalScriptEditor(string path)
    {
      EditorPrefs.SetString("kScriptsDefaultApp", path);
    }
    
  }
}