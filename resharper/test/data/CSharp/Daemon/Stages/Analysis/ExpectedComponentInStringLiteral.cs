using UnityEngine;

public class Test
{
    public void Method(GameObject go)
    {
        // Built in components must derive from Component
        go.AddComponent("Caching");
        go.GetComponent("Caching");

        go.AddComponent("Grid");
        go.GetComponent("Grid");
    }
}
