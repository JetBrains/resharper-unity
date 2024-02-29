using UnityEngine;

public class Test : MonoBehaviour
{
    public void Method(GameObject go, Component c, Transform t, Object o)
    {
        if (go == null) return;
        if (c == null) return;
        if (t == null) return;
        if (o == null) return;
        if (go != null) return;
        if (c != null) return;
        if (t != null) return;
        if (o != null) return;
        if (null != o) return;
        if (null == go) return;
        if (this == null) return;
        if (o != go) return;
        if (o == t) return;
    }
}