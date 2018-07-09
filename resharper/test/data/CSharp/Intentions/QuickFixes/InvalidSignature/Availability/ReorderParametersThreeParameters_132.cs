using UnityEngine;
using UnityEditor;

public class FooProcessor : AssetPostprocessor
{
    private void OnPostprocessGameObjectWithUserProperties({caret}GameObject go, object[] values, string[] propNames)
    {
    }
}