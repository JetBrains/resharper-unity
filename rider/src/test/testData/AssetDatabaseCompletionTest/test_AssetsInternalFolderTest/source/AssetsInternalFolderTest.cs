using UnityEditor;

public class UnityResourcesLoadCompletion : MonoBehaviour
{
    void Start()
    {
        AssetDatabase.LoadMainAssetAtPath("Assets/Editor/Resources/<caret>")
    }
}
