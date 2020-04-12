using UnityEngine;

public class Test
{
    public void Method(GameObject go)
    {
        // This won't complete, because we don't complete ALL types, and this resolves to UnityEngine.Grid
        go.GetComponent("Gri{caret}");
    }
}
