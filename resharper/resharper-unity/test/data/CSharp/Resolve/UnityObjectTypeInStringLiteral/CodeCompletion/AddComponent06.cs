using UnityEngine;

public class Test
{
    public void Method(GameObject go)
    {
        go.AddComponent("My{caret}");
    }
}

public class MyMonoBehaviour : MonoBehaviour
{
}
