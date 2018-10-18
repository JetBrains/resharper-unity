using System;
using UnityEngine;

public class Test
{
    [Serialize{caret}Field] private readonly int myValue1 = 42;

    public void Thing()
    {
        Console.WriteLine(myValue1);
    }
}
