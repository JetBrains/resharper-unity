using UnityEngine;
using UnityEditor;

public class FooProcessor : AssetPostprocessor
{
    private void OnPostprocessGameObjectWithUserProperties({caret}string[] propNames, GameObject go, object[] values)
    {
    }
}