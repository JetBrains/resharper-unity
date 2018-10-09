using UnityEngine;

public class Test
{
    public void Method()
    {
        // This won't complete, because we don't complete ALL types, and this resolves to UnityEngine.Grid
        var so = ScriptableObject.CreateInstance("Edit{caret}");
    }
}
