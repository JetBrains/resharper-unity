using UnityEngine;

public class Test
{
    public void Method()
    {
        var so = ScriptableObject.CreateInstance("TheNamespace.My{caret}");
    }
}

namespace TheNamespace
{
    public class MyScriptableObject : ScriptableObject
    {
    }
}
