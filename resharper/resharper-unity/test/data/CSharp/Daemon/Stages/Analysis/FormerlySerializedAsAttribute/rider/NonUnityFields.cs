using UnityEngine;
using UnityEngine.Serialization;

public class Test01
{
    [FormerlySerializedAs("foo")] private int myNotUnityType;
    [FormerlySerializedAs("foo2")] public string field1;
    [FormerlySerializedAs("foo3")] public const string constant1;
}

public class Test02 : MonoBehaviour
{
    [FormerlySerializedAs("foo")] private int myNotSerialized;
    [FormerlySerializedAs("foo2")] public string field1;
    [FormerlySerializedAs("foo3")] public const string constant1;
}
