using UnityEngine;

public class Foo : MonoBehaviour
{
    public GameObject Method(GameObject o)
    {
        return o{caret} ?? gameObject;
    }

    public Transform Method2(Transform t)
    {
        return t ?? transform;
    }

    public Component Method3(Component c1, Component c2)
    {
        var safeC1 = c1 ?? new Component();
        return c2 ?? safeC1;
    }
}

public class Foo2 : MonoBehaviour
{
    public GameObject Method(GameObject o)
    {
        return o ?? this.gameObject;
    }
}
