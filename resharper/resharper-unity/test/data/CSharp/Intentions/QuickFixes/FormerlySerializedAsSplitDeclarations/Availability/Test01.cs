using UnityEngine;
using UnityEngine.Serialization;

public class Test : MonoBehaviour
{
  [Formerly{caret}SerializedAs("foo")] public int value1, value2, value3;
}
