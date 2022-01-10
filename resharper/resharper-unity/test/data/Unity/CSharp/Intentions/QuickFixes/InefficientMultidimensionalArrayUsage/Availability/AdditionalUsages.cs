using UnityEditor;

public class Foo
{
    [InitializeOnLoadMethod]
    public static int OnLoad()
    {
        var x = new in{caret}t[5, 5];

        var y = x;
        return 0;
    }
}
