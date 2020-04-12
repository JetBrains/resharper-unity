using UnityEngine;

public class MyScriptableObject : ScriptableObject
{
}

public class Whatever
{
    public static void DoSomething()
    {
        var so = new MyScriptableObject();
    }
}
