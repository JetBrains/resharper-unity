using UnityEngine;

public class Test
{
    public void Method(GameObject go)
    {
        // This won't include MyMonoBehaviour! We don't show ALL types
        go.GetComponent("My{caret}");
    }
}

namespace TheNamespace
{
    public class MyMonoBehaviour : MonoBehaviour
    {
    }
}
