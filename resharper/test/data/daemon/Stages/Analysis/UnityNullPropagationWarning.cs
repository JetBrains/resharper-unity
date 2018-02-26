using UnityEngine;

public class Test : MonoBehaviour
{
    public void Method(GameObject go, Component c, Transform t, Object o)
    {
        var name = o?.name;
        name = go?.name;
        name = c?.name;
        name = t?.name;
        name = gameObject?.name;
        name = this.gameObject?.name;
        name = transform?.name;
        name = this.transform?.name;
    }
}
