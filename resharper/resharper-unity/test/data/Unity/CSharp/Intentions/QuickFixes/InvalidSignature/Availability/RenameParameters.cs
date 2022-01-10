using System;
using UnityEngine;

public class Foo : MonoBehaviour
{
    public void OnAudioFilterRead(int c, f{caret}loat[] d)
    {
        Console.WriteLine(c);
        Console.WriteLine(d);
    }
}
