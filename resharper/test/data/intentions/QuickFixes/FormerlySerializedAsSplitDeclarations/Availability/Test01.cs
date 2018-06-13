using UnityEngine;
using UnityEngine.Serialization;

public class Test : MonoBehaviour
{
  [Formerly{caret}SerializedAs("foo")] public int Value1, Value2, Value3;
}
