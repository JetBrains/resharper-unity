using UnityEngine;

public class Test
{
    public void Method()
    {
        // This won't include MyMonoBehaviour! We don't show ALL types
        var so = ScriptableObject.CreateInstance("My{caret}");
    }
}

namespace TheNamespace
{
    public class MyScriptableObject : ScriptableObject
    {
    }
}
