// ${RUN:2}
using System;
using UnityEngine;

public class Test
{
    [Serialize{caret}Field] [NonSerialized] public int Value1;
    [SerializeField] [NonSerialized] public int Value2;
    [SerializeField] [NonSerialized] public int Value3;
    [SerializeField] [NonSerialized] public int Value4;
}

public class Test2
{
    [SerializeField] [NonSerialized] public int Value1;
    [SerializeField] [NonSerialized] public int Value2;
    [SerializeField] [NonSerialized] public int Value3;
    [SerializeField] [NonSerialized] public int Value4;
}
