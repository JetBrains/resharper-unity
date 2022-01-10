// ${RUN:2}
using System;
using UnityEngine;
using UnityEngine.Serialization;

public class Test : MonoBehaviour
{
    [FormerlySerializedAs("NotSerializedField1")] public static int Value1;
    [FormerlySerializedAs("NotSerializedField2")] [NonSerialized] public int Value2;
    [FormerlySerializedAs("RedundantValue")] [NonSerialized] public int RedundantValue;
    [Formerly{caret}SerializedAs("RedundantValue2"), FormerlySerializedAs("RedundantValue2")] [NonSerialized] public int RedundantValue2;
}

public class Test2 : MonoBehaviour
{
    [FormerlySerializedAs("RedundantValue")] [NonSerialized] public int RedundantValue;
}

public struct Tes3
{
    [SerializeField, FormerlySerializedAs("Foo")] public int Bar;
}
