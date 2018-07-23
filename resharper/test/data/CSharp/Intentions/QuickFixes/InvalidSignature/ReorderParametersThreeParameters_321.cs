using UnityEngine;
using UnityEditor;

public class FooProcessor : AssetPostprocessor
{
    private void OnPostprocessGameObjectWithUserProperties({caret}object[] values, string[] propNames, GameObject go)
    {
    }
}