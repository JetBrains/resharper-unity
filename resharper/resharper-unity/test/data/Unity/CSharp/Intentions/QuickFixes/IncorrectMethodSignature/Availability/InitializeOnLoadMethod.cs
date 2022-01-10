using UnityEditor;

public class Foo
{
    [InitializeOnLoadMethod]
    public int On{caret}Load(int value)
    {
        return 42;
    }
}
