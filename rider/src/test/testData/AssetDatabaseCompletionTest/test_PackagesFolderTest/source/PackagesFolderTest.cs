using UnityEditor;

public class UnityResourcesLoadCompletion : MonoBehaviour
{
    void Start()
    {
        AssetDatabase.LoadMainAssetAtPath("Packages/<caret>")
    }
}
