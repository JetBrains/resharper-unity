using System.IO;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [UsedImplicitly]
    public static class IntegrationTestHelper
    {
        private static bool myChanged;
        private static object myLockObject = new Object();

        private static string myEditorPluginFolder = Path.Combine(Application.dataPath, "Plugins", "Editor", "JetBrains");
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
        }

        public static void ResetAndStart()
        {
            EditorPrefs.DeleteAll();
            Start();
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