using UnityEngine;

public class Test
{
    public void Method(GameObject go)
    {
        go.GetComponent("My{caret}");
    }
}

public class MyMonoBehaviour : MonoBehaviour
{
}
