using System;
using UnityEngine;

public class Test
{
    [Obsolete("Needed to legalize obsoleted AddComponent invocation")]
    public void Method(GameObject go)
    {
        // Built in components must derive from Component
        go.AddComponent("Caching");
        go.GetComponent("Caching");

        go.AddComponent("Grid");
        go.GetComponent("Grid");
    }
}
