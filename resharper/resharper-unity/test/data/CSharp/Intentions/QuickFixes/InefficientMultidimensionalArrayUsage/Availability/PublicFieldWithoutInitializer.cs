using UnityEditor;

public class Foo
{
    public int[,] myTest = new in{caret}t[5, 5];
    [InitializeOnLoadMethod]
    public static int OnLoad()
    {
        return 0;
    }
}
