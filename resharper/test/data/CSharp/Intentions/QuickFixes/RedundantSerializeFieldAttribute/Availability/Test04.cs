using System;
using UnityEngine;

public class Test
{
    [Serialize{caret}Field] private const int Value1 = 42;

    public void Thing()
    {
        Console.WriteLine(Value1);
    }
}
