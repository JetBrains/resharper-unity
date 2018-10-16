using System;
using UnityEngine;

public class Test
{
    [Serialize{caret}Field] private static int ourValue1 = 42;

    public void Thing()
    {
        ourValue1 = 42;
        Console.WriteLine(ourValue1);
    }
}
