using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Put the file to Assets\Plugins\Editor

namespace Assets.Plugins.Editor
{
  [InitializeOnLoad]
  public static class Rider
  {
    static Rider()
    {
      var riderPath = EditorPrefs.GetString("kScriptsDefaultApp");
      
      if (riderPath != null)
      {
        var riderFileInfo = new FileInfo(riderPath);
        if (riderPath.ToLower().Contains("rider") && !riderFileInfo.Exists)
        {
          var newPath = riderPath;
          // try to search the new version

          switch (riderFileInfo.Extension)
          {

            /*
            Unity itself transforms lnk to exe
            case ".lnk":
            {
              if (riderFileInfo.Directory != null && riderFileInfo.Directory.Exists)
              {
                var possibleNew = riderFileInfo.Directory.GetFiles("*ider*.lnk");
                if (possibleNew.Length > 0)
                  newPath = possibleNew.OrderBy(a => a.LastWriteTime).Last().FullName;
              }
              break;
            }*/
            case ".exe":
            {
              var possibleNew =
                riderFileInfo.Directory.Parent.Parent.GetDirectories("*ider*")
                  .SelectMany(a => a.GetDirectories("bin")).SelectMany(a=>a.GetFiles(riderFileInfo.Name))
                  .ToArray();
              if (possibleNew.Length > 0)
                newPath = possibleNew.OrderBy(a => a.LastWriteTime).Last().FullName;
              break;
            }
            default:
            {
              Debug.Log("Please manually update the path to Rider in Unity Preferences -> External Tools -> External Script Editor.");
              break;
            }
          }
          if (newPath != riderPath)
          {
            Debug.Log(riderPath);
            Debug.Log(newPath);
            EditorPrefs.SetString("kScriptsDefaultApp", newPath);
          }
        }
      }

      Debug.Log("Attempt to update settings");
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
        Debug.LogError(e.Message);
      }
    }
  }

  public class RiderAssetPostprocessor : AssetPostprocessor
  {
    public static void OnGeneratedCSProjectFiles()
    {
      var currentDirectory = Directory.GetCurrentDirectory();
      var projectFiles = Directory.GetFiles(currentDirectory, "*.csproj");

      bool isModified = false;
      foreach (var file in projectFiles)
      {
        string content = File.ReadAllText(file);
        if (content.Contains("<TargetFrameworkVersion>v3.5</TargetFrameworkVersion>"))
        {
          content = Regex.Replace(content, "<TargetFrameworkVersion>v3.5</TargetFrameworkVersion>",
            "<TargetFrameworkVersion>v4.5</TargetFrameworkVersion>");
          File.WriteAllText(file, content);
          isModified = true;
        }
      }

      Debug.Log(isModified ? "Project was post processed successfully" : "No change necessary in project");
    }
  }
}

// Developed using JetBrains Rider =)