using UnityEngine;
using UnityEngine.Serialization;

public class Test01
{
    [FormerlySerializedAs("myValue")] private int myValue2;
    [FormerlySerializedAs("myConstant")] private const int myValue3 = 42;
    [FormerlySerializedAs("myStatic")] private static int myValue4 = 42;
}

public class Test02 : MonoBehaviour
{
    [FormerlySerializedAs("myValue"), FormerlySerializedAs("foo")] public int myValue;
}
