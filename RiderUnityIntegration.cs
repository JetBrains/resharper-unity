using System;
using System.IO;
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
			var files = new DirectoryInfo(".").GetFiles("*.csproj");

			bool isModified = false;
			foreach (var file in files)
			{
				if (ReplaceInFile(file.FullName, "<TargetFrameworkVersion>v3.5</TargetFrameworkVersion>",
					"<TargetFrameworkVersion>v4.5</TargetFrameworkVersion>"))
					isModified = true;
			}

			if (isModified)
				Debug.Log("Project was post processed successfully");
			else
				Debug.Log("No change necessary in project");
		}

		private static bool ReplaceInFile(string filePath, string searchText, string replaceText)
		{
			string oldText = File.ReadAllText(filePath);
			string newText = oldText.Replace(searchText, replaceText);
			if (newText == oldText) return false;
			File.WriteAllText(filePath, newText);
			return true;
		}
	}
}
