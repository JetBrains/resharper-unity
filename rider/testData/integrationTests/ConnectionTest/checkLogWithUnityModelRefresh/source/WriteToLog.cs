using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class DumpExternalEditor
{
    private static bool isReported = false;
    static DumpExternalEditor()
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
}
