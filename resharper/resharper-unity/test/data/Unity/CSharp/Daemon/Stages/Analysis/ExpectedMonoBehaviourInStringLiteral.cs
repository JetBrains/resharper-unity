using UnityEngine;

public class Test
{
    public void Method(GameObject go)
    {
        // User components must implement MonoBehaviour
        go.AddComponent("MyPlainOldClass");
        go.GetComponent("MyPlainOldClass");

        go.AddComponent("MyComponent");
        go.GetComponent("MyComponent");

        go.AddComponent("MyMonoBehaviour");
        go.GetComponent("MyMonoBehaviour");
    }
}

public class MyPlainOldClass
{
}

public class MyComponent : Component
{
}

public class MyMonoBehaviour : MonoBehaviour
{
}
