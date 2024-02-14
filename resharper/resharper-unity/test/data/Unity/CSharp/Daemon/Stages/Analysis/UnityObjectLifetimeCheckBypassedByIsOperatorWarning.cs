using UnityEngine;

public class Test : MonoBehaviour
{
    public void Method(GameObject go, Component c, Transform t, Object o)
    {
        if (go is null) return;
        if (c is null) return;
        if (t is null) return;
        if (o is null) return;
        if (go is not null) return;
        if (c is not null) return;
        if (t is not null) return;
        if (o is not null) return;
        if (go is {}) return;
        if (c is {}) return;
        if (t is {}) return;
        if (o is {}) return;
        if (go is not {}) return;
        if (c is not {}) return;
        if (t is not {}) return;
        if (o is not {}) return;
        if (go is {name: ""}) return;
        if (c is {name: ""}) return;
        if (t is {name: ""}) return;
        if (o is {name: ""}) return;
        if (go is not {name: ""}) return;
        if (c is not {name: ""}) return;
        if (t is not {name: ""}) return;
        if (o is not {name: ""}) return;
    }
}
