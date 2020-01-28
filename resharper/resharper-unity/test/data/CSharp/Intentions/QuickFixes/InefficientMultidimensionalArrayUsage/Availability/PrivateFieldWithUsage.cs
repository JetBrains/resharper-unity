using UnityEditor;
using UnityEngine;

public class Foo : MonoBehaviour
{
    private int[,] myTest = new in{caret}t[5, 5];
    [InitializeOnLoadMethod]
    public static int OnLoad()
    {
        return 0;
    }

    public void Update() {
        myTest[0, 0] = 5;
    }
}
