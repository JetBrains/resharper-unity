using UnityEditor;

public class Foo
{
    private int[,] myTest = new in{caret}t[5, 5]
    [InitializeOnLoadMethod]
    public static int OnLoad()
    {
        myTest[0, 0] = 10;
        return 0;
    }
}
