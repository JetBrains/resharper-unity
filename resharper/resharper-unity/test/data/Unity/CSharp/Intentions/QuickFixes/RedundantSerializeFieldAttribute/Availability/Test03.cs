using System;
using UnityEngine;

public class Test
{
    // ReSharper disable once ConvertToConstant.Local
    [Serialize{caret}Field] private readonly int myValue1 = 42;

    public void Thing()
    {
        Console.WriteLine(myValue1);
    }
}
