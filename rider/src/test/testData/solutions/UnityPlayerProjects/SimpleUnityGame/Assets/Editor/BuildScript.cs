using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;

public class BuildScript
{
    [MenuItem("Build/BuildProject")]
     public static void Build()
     {
         string outputPath = Path.Combine(Application.dataPath, "../Builds");
         outputPath = Path.GetFullPath(outputPath);
         if (!Directory.Exists(outputPath))
             Directory.CreateDirectory(outputPath);

        //read arguments
        var args = System.Environment.GetCommandLineArgs();
        string backendArg = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-backend" && i + 1 < args.Length)
            {
                backendArg = args[i + 1];
            }
        }

        // change backend
        if (!string.IsNullOrEmpty(backendArg))
        {
            if (backendArg.Equals("IL2CPP", System.StringComparison.OrdinalIgnoreCase))
            {
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
                Debug.Log("Using backend: IL2CPP");
            }
            else if (backendArg.Equals("Mono", System.StringComparison.OrdinalIgnoreCase))
            {
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
                Debug.Log("Using backend: Mono");
            }
            else
            {
                Debug.LogWarning($"Unknown backend '{backendArg}', fallback to project default");
            }
        }


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
