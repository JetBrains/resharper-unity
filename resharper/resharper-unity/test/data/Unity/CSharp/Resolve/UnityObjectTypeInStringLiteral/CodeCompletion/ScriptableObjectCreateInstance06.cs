using UnityEngine;

public class Test
{
    public void Method()
    {
        var so = ScriptableObject.CreateInstance("My{caret}");
    }
}

public class MyScriptableObject : ScriptableObject
{
}
