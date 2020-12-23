using System.IO;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor
{
    [UsedImplicitly]
    public static class IntegrationTestHelper
    {
        private static bool myChanged;
        private static object myLockObject = new Object();

        private static string myEditorPluginFolder = Path.Combine(Application.dataPath, "Plugins/Editor/JetBrains");
        private static string myEditorPlugin = Path.Combine(myEditorPluginFolder, "JetBrains.Rider.Unity.Editor.Plugin.Repacked.dll");

        public static void Start()
        {
            if (!File.Exists(myEditorPlugin)) 
            {
                var fileSystemWatcher = new FileSystemWatcher(myEditorPluginFolder, "*.dll");
                fileSystemWatcher.Created += (sender, args) =>
                {
                    lock (myLockObject)
                    {
                        myChanged = true;
                        fileSystemWatcher.Dispose();
                    }
                };
                fileSystemWatcher.EnableRaisingEvents = true;
                EditorApplication.update += Update;
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