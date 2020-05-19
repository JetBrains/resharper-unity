// ${RUN:2}
using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEditor;

[assembly: SerializeField]

[SerializeFie{caret}ld]
public class Foo : MonoBehaviour
{
    [CustomEditor(typeof(Material))]
    private int myField;

    [FormerlySerializedAs("Foo")] public static int Value1;
    [FormerlySerializedAs("Bar")] [NonSerialized] public int Value2;
    [FormerlySerializedAs("RedundantValue")] public int RedundantValue;
    [FormerlySerializedAs("RedundantValue2"), FormerlySerializedAs("RedundantValue2")] public int RedundantValue2;

    [HideInInspector] [NonSerialized] public int Value1;
    [HideInInspector] private int Value2;
    [HideInInspector] public const int Value3 = 42;

    [SerializeField] [NonSerialized] public int Value1;
    [SerializeField] [NonSerialized] public int Value2;
    [SerializeField] [NonSerialized] public int Value3;
    [SerializeField] [NonSerialized] public int Value4;
}

public class Test2 : MonoBehaviour
{
    [FormerlySerializedAs("RedundantValue")] [NonSerialized] public int RedundantValue;

    [HideInInspector] public static int Value1 = 42;
    [HideInInspector] public readonly int Value2 = 42;
}

public struct Tes3
{
    [SerializeField, FormerlySerializedAs("Foo")] public int Bar;

    [SerializeField] [NonSerialized] public int Value1;
    [SerializeField] [NonSerialized] public int Value2;
    [SerializeField] [NonSerialized] public int Value3;
    [SerializeField] [NonSerialized] public int Value4;
}

[HideInInspector]
public delegate void MyEventHandler(object sender, EventArgs e);

[InitializeOnLoad]
public class MissingConstructor
{
}

[InitializeOnLoad]
public class MissingConstructor2
{
}

[InitializeOnLoad]
public class MissingConstructor3
{
}
