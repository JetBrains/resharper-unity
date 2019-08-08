using System.IO;
using UnityEditor;
using UnityEngine;
using File = System.IO.File;

namespace Editor
{
    [InitializeOnLoad]
    public static class IntegrationTestHelper
    {
        private static bool myIsEditorPluginLoad;
        private static string myPath = Path.Combine(Application.dataPath.Replace('/', '\\'), "Plugins", "Editor", "JetBrains", "JetBrains.Rider.Unity.Editor.Plugin.Repacked.dll");
        
        static IntegrationTestHelper()
        {
            File.Create(".start"); 
            EditorApplication.update += Update;
            Debug.Log(myPath);
        }

        private static void Update()
        {
            if (!myIsEditorPluginLoad && File.Exists(myPath))
            {
                myIsEditorPluginLoad = true;
                Debug.Log("Refresh plugin...");
                AssetDatabase.Refresh();
            }
        }
    }
}