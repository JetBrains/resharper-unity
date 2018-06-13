using System;
using UnityEngine;

public class Foo : MonoBehaviour
{
    [NonSerialized] public int myValue, my{caret:Make:field:'myValue2':serialized}Value2, myValue3;
}
