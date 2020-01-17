using UnityEngine;
using UnityEditor;

public class Foo : MonoBehaviour
{
    public void Update()
    {
        int[,] a, b = new in{caret}t[5, 5];

        b[0, 0] = 5;
        a[1, 2] = 4;
        return 0;
    }
}
