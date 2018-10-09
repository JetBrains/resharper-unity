using UnityEngine;

public class Test
{
    public void Method()
    {
        var so = ScriptableObject.CreateInstance("WrongBaseType");
        so = ScriptableObject.CreateInstance("MyScriptableObject");
    }
}

public class WrongBaseType
{
}

public class MyScriptableObject : ScriptableObject
{
}
