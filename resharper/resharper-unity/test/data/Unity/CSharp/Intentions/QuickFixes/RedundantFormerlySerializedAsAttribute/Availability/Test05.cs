using UnityEngine;
using UnityEngine.Serialization;

public class Test : MonoBehaviour
{
    [Formerly{caret}SerializedAs("NotSerializedField")] public readonly int Value = 42;
}
