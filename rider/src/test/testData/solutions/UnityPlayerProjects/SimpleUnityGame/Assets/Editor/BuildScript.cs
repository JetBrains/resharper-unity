using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public class BuildScript
{
    [MenuItem("Build/BuildProject")]
     public static void Build()
     {
         string outputPath = Path.Combine("Builds");
         if (!Directory.Exists(outputPath))
             Directory.CreateDirectory(outputPath);

         BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
         {
             scenes = new[] { "Assets/Scenes/SampleScene.unity" },
             locationPathName = Path.Combine(outputPath, GetExecutableName()),
             target = EditorUserBuildSettings.activeBuildTarget,
             options = BuildOptions.Development | BuildOptions.AllowDebugging
         };

         BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
         BuildSummary summary = report.summary;

         if (summary.result == BuildResult.Succeeded)
         {
             Debug.Log($"Build succeeded");
         }
         else if (summary.result == BuildResult.Failed)
         {
             Debug.LogError("Build failed");
         }
     }

    private static string GetExecutableName()
    {
        return Application.platform switch
        {
            RuntimePlatform.WindowsEditor => "SimpleUnityGame.exe",
            RuntimePlatform.OSXEditor => "SimpleUnityGame.app",
            _ => "SimpleUnityGame"
        };
    }
}
