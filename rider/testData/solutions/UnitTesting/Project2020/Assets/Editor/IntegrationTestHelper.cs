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
    public static class IntegrationTestHelper
    {
        private static bool myChanged;
        private static object myLockObject = new Object();

        public static void Start()
        {
            var commandlineParser = new CommandLineParser(Environment.GetCommandLineArgs());
            if (commandlineParser.Options.ContainsKey("-riderPath"))
            {
                var originRiderPath = commandlineParser.Options["-riderPath"];
                Debug.Log($"CodeEditor.SetExternalScriptEditor({originRiderPath});");
                CodeEditor.SetExternalScriptEditor(originRiderPath);
            }

            File.WriteAllText(".start", string.Empty);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
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