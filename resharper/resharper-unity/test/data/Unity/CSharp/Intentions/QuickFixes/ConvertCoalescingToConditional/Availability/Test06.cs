using UnityEngine;

public class Foo : MonoBehaviour
{
    public GameObject Method(GameObject o)
    {
        o {caret}??= gameObject;
        return o;
    }

    public Transform Method2(Transform t)
    {
        t ??= transform;
        return t;
    }

    public Component Method3(Component c1, Component c2)
    {
        var safeC1 = c1;
        safeC1 ??= new Component();
        c2 ??= safeC1;
        return c2;
    }
}

public class Foo2 : MonoBehaviour
{
    public GameObject Method(GameObject o)
    {
        o ??= this.gameObject;
        return o;
    }
}
