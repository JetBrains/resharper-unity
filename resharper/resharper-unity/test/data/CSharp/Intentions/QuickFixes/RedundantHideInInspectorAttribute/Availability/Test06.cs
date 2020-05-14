// ${RUN:2}
using System;
using UnityEngine;

public class Test
{
    [HideIn{caret}Inspector] [NonSerialized] public int Value1;
    [HideInInspector] private int Value2;
    [HideInInspector] public const int Value3 = 42;
}

public class Test2
{
    [HideInInspector] public static int Value1 = 42;
    [HideInInspector] public readonly int Value2 = 42;
}
