using System;
using System.IO;
using JetBrains.Annotations;
using Packages.Rider.Editor.Util;
using Unity.CodeEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Editor
{
    public static class BuildHelper
    {
   
        [MenuItem("Build Tools/Local Windows Build")]
        public static void LocalBuildWindows()
        {
            RunLocalDebugBuild(BuildTarget.StandaloneWindows64);
        }        
        
        [MenuItem("Build Tools/Local OSX Build")]
        public static void LocalBuildMac()
        {
            RunLocalDebugBuild(BuildTarget.StandaloneOSX);
        }

        private static void RunLocalDebugBuild(BuildTarget buildTarget)
        {
            var projectName = Application.productName;
            var folderName = $"UnityPlayerDebuggerTest_{buildTarget}_{Application.unityVersion}_{DateTime.Now:yyyy-MMM-dd}";
            Build(buildTarget, false, true, $"{folderName}/{projectName}.exe");
        }

        public static void RunBuild()
        {
            EditorPrefs.SetInt("Rider_SelectedLoggingLevel", 6); // trace
            var commandlineParser = new CommandLineParser(Environment.GetCommandLineArgs());
            var buildPath = GetBuildPath(commandlineParser);
            var isDebug = GetDebugBuildValue(commandlineParser);
            var isIl2Cpp = GetIl2CppValue(commandlineParser);
            var buildTarget = GetBuildTargetValue(commandlineParser);

            Build(buildTarget, isIl2Cpp, isDebug, buildPath);
        }

        private static BuildTarget GetBuildTargetValue(CommandLineParser commandlineParser)
        {
            const string buildTargetArgName = "--buildTarget";
            if (commandlineParser.Options.TryGetValue(buildTargetArgName, out var value))
            {
                if (Enum.TryParse(value, out BuildTarget buildTarget))
                {
                    return buildTarget;
                }
            }

            throw new ArgumentException($"The {buildTargetArgName} must be specified");
        }

        private static bool GetIl2CppValue(CommandLineParser commandlineParser)
        {
            if (commandlineParser.Options.TryGetValue("--isIl2Cpp", out var value))
            {
                if (bool.TryParse(value, out var isIl2Cpp))
                {
                    return isIl2Cpp;
                }
            }

            return false;
        }

        private static bool GetDebugBuildValue(CommandLineParser commandlineParser)
        {
            if (commandlineParser.Options.TryGetValue("--isDebug", out var value))
            {
                if (bool.TryParse(value, out var isDebug))
                {
                    return isDebug;
                }
            }

            return false;
        }

        private static string GetBuildPath(CommandLineParser commandlineParser)
        {
            const string buildPathArg = "-buildPath";
            if (commandlineParser.Options.TryGetValue(buildPathArg, out var buildPath))
            {
                Console.WriteLine($"Build directory exists {Directory.Exists(buildPath)}");
                return buildPath;
            }

            throw new ArgumentException($"The {buildPathArg} must be specified");
        }

        private static void Build(BuildTarget targetPlatform, bool il2CPP, bool developmentBuild,
            string path = "build/game.exe")
        {
            Debug.Log("Starting the build process...");

            // Define the build options
            var buildOptions = BuildOptions.None;

            if (developmentBuild)
            {
                buildOptions |= BuildOptions.Development | BuildOptions.AllowDebugging;
                EditorUserBuildSettings.SetPlatformSettings("Standalone", "CopyPDBFiles", true.ToString());
            }
            buildOptions |= BuildOptions.CleanBuildCache;

            if (il2CPP)
            {
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
            }
            else
            {
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
            }

            var report = BuildPipeline.BuildPlayer(EditorBuildSettings.scenes, path, targetPlatform, buildOptions);

            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log("Build succeeded: " + report.summary.totalSize + " bytes");
                
                Debug.Log("Target platform: " + report.summary.platform);
                Debug.Log("Build options: " + report.summary.options);
            }
            else if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Failed)
            {
                Debug.Log("Build failed");
            }
        }
    }

    [UsedImplicitly]
    [InitializeOnLoad]
    public static class IntegrationTestHelper
    {
        static IntegrationTestHelper()
        {
            Console.WriteLine("IntegrationTestHelper.ctor");
        }

        private static bool myChanged;
        private static object myLockObject = new Object();

        public static void Start()
        {
            Debug.Log($"Before kAutoRefresh = {EditorPrefs.GetBool("kAutoRefresh")}");
            EditorPrefs.SetBool("kAutoRefresh", true); // false by default for some reason?
            Debug.Log($"After kAutoRefresh = {EditorPrefs.GetBool("kAutoRefresh")}");

            EditorPrefs.SetInt("Rider_SelectedLoggingLevel", 6); // trace
            var commandlineParser = new CommandLineParser(Environment.GetCommandLineArgs());
            if (commandlineParser.Options.ContainsKey("-riderPath"))
            {
                var originRiderPath = commandlineParser.Options["-riderPath"];
                Console.WriteLine($"originRiderPath file exists {File.Exists(originRiderPath)}");
                Console.WriteLine($"originRiderPath directory exists {Directory.Exists(originRiderPath)}");
                Debug.Log($"CodeEditor.SetExternalScriptEditor({originRiderPath});");
                CodeEditor.SetExternalScriptEditor(originRiderPath);
            }

            File.WriteAllText(".start", string.Empty);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            EditorUtility
                .RequestScriptReload(); // EditorPlugin would get loaded // todo: it must be reloaded itself, but for some reason it doesn't make it during te test
        }

        public static void ResetAndStart()
        {
            EditorPrefs.DeleteAll();
            Start();
        }

        private static bool isReported = false;

        public static void WriteToLog()
        {
            EditorApplication.update += () =>
            {
                if (!isReported)
                {
                    Debug.Log("#Test#");
                    isReported = true;
                }
            };
        }

        public static void DumpExternalEditor()
        {
            var path = Path.Combine(Application.dataPath, "ExternalEditor.txt");
            File.WriteAllText(path, EditorPrefs.GetString("kScriptsDefaultApp", null) ?? "Unknown");
        }

        private static void Update()
        {
            var localChange = myChanged;
            lock (myLockObject)
            {
                myChanged = false;
            }

            if (localChange)
            {
                Debug.Log("Recompile for plugin load");
                AssetDatabase.Refresh();
            }
        }
    }
}