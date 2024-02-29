using UnityEngine;

public class Test : MonoBehaviour
{
    public void Method(GameObject go, Component c, Transform t, Object o)
    {
        var go2 = go;
        o ??= null;
        c ??= null;
        go ??= gameObject;
        go ??= this.gameObject;
        t ??= null;
        t ??= transform;
        t ??= this.transform;
        go ??= null;
        o ??= go2;
    }
}
