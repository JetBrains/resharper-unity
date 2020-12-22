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
            EditorUtility.RequestScriptReload(); // EditorPlugin would get loaded // todo: it must be reloaded itself, but for some reason it doesn't make it during te test
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