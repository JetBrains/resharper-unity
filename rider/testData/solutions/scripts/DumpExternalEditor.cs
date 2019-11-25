using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class DumpExternalEditor 
{
    static DumpExternalEditor()
    {
        var path = Path.Combine(Application.dataPath.Replace('/', '\\'), "ExternalEditor.txt");
        File.WriteAllText(path, EditorPrefs.GetString("kScriptsDefaultApp") ?? "Unknown");
    }
}
