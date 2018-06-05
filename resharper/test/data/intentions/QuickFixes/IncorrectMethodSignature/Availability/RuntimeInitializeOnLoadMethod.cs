using UnityEngine;

public class Foo
{
    [RuntimeInitializeOnLoadMethod]
    public int On{caret}Load(int value)
    {
        return 42;
    }
}
