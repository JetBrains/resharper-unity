using UnityEngine;

public class Test : MonoBehaviour
{
    public void Method(GameObject go, Component c, Transform t, Object o)
    {
        var obj = o ?? null;
        obj = c ?? null;
        obj = go ?? gameObject;
        obj = go ?? this.gameObject;
        obj = t ?? null;
        obj = t ?? transform;
        obj = t ?? this.transform;
        obj = go ?? null;
        obj = gameObject ?? null;
        obj = transform ?? null;
        obj = this.gameObject ?? null;
        obj = this.transform ?? null;
    }
}
