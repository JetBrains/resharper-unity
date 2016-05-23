using UnityEngine;
using UnityEditor;
using System;
using System.IO;

public class RiderUnityIntegration : AssetPostprocessor
{
  public static void OnGeneratedCSProjectFiles()
  {
    // Open the solution file
    string projectDirectory = Directory.GetParent(Application.dataPath).FullName;
    string projectName = Path.GetFileName(projectDirectory);
    string slnFile = Path.Combine(projectDirectory, string.Format("{0}.sln", projectName));

    try
    {
      EditorPrefs.SetString("kScriptEditorArgs", "\"" + slnFile + "\"" + " -l $(Line) " + "\"" + "$(File)" + "\"");
    }
    catch (Exception e)
    {
      Console.WriteLine(e.Message);
    }
  }
}