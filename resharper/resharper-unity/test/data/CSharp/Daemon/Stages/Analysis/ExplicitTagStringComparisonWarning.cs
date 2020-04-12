using UnityEngine;

public class Test : MonoBehaviour
{
    public void Method(GameObject go, Component c, Foo f)
    {
        if (go.tag == "Whatever") { }
        if (go.tag != "Whatever") { }
        if (c.tag == "Whatever") { }
        if (c.tag != "Whatever") { }
        if (this.tag == "Whatever") { }
        if (this.tag != "Whatever") { }
        if (tag == "Whatever") { }
        if (tag != "Whatever") { }
        if (f.tag == "Whatever") { }
        var other = "Whatever";
        if (other == go.tag) { }
        if (c.tag == go.tag) { }
        if (c.tag == null) { }
        if (c.tag == 12) { }
    }
}
