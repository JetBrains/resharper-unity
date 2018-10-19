using UnityEngine;
using UnityEngine.Serialization;

public class Test : MonoBehaviour
{
    [Formerly{caret}SerializedAs("NotSerializedField")] public static int Value;
}
