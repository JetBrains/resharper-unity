using UnityEngine;

public class Foo : MonoBehaviour
{
    public void Method()
    {
        if (t{caret}ag == "Whatever" || tag == "Something") { }
    }

    public bool Method2()
    {
        return tag == "Test";
    }
}

public class Foo2 : MonoBehaviour
{
    public bool Method()
    {
        return tag == "Other";
    }
}
