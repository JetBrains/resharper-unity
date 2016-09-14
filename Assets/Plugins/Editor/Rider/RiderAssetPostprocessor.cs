using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Assets.Plugins.Editor.Rider
{
    public class RiderAssetPostprocessor : AssetPostprocessor
    {
        public static void OnGeneratedCSProjectFiles()
        {
            if (!Rider.Enabled)
                return;
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

            var slnFiles = Directory.GetFiles(currentDirectory, "*.sln"); // piece from MLTimK fork
            foreach (var file in slnFiles)
            {
                string content = File.ReadAllText(file);
                const string magicProjectGUID = @"Project(""{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}"")";
                // guid representing C# project
                if (!content.Contains(magicProjectGUID))
                {
                    string matchGUID = @"Project\(\""\{[A-Z0-9]{8}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{12}\}\""\)";
                    // Unity may put a random guid, which will brake Rider goto
                    content = Regex.Replace(content, matchGUID, magicProjectGUID);
                    File.WriteAllText(file, content);
                    isModified = true;
                }
            }

            Debug.Log(isModified ? "Project was post processed successfully" : "No change necessary in project");

            try
            {
                if (slnFiles.Any())
                    EditorPrefs.SetString("kScriptEditorArgs", "\"" + slnFiles.First() + "\"");
                else
                    EditorPrefs.SetString("kScriptEditorArgs", string.Empty);
            }
            catch (Exception e)
            {
                Debug.Log("Exception on updating kScriptEditorArgs: " + e.Message);
            }
        }
    }
}