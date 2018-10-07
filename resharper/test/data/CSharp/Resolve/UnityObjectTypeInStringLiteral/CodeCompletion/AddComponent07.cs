using UnityEngine;

public class Test
{
    public void Method(GameObject go)
    {
        go.AddComponent("TheNamespace.My{caret}");
    }
}

namespace TheNamespace
{
    public class MyMonoBehaviour : MonoBehaviour
    {
    }
}
