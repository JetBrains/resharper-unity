using UnityEngine;
using UnityEngine.Serialization;

public class Test01
{
    [FormerlySerializedAs("foo")] private int myNotUnityType;
}

public class Test02 : MonoBehaviour
{
    [FormerlySerializedAs("foo")] private int myNotSerialized;
}
