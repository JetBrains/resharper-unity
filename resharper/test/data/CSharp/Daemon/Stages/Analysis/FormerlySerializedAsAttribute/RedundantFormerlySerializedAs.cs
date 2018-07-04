using UnityEngine;
using UnityEngine.Serialization;

public class Test01
{
    [FormerlySerializedAs("myValue")] private int myValue2;
}

public class Test02 : MonoBehaviour
{
    [FormerlySerializedAs("myValue"), FormerlySerializedAs("foo")] public int myValue;
}
