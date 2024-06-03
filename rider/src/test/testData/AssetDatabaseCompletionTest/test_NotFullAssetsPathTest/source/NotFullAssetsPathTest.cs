using UnityEditor;

public static class AssetDatabaseLoadAssetAtPath
{
    static void JustDoIt()
    {
        var loadAssetAtPath = AssetDatabase.LoadMainAssetAtPath("A<caret>");
    }
}
