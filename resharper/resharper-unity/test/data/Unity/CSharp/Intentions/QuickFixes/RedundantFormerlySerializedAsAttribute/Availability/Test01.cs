using System;
using UnityEngine;
using UnityEngine.Serialization;

public class Test : MonoBehaviour
{
    [Formerly{caret}SerializedAs("foo")] [NonSerialized] public int Value;
}
