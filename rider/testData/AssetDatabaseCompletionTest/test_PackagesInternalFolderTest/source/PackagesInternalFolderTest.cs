using UnityEditor;

public class UnityResourcesLoadCompletion : MonoBehaviour
{
    void Start()
    {
        AssetDatabase.LoadMainAssetAtPath("Packages/com.jetbrains.from_git/Runtime/<caret>")
    }
}
