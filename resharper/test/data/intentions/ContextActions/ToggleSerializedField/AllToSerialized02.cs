using System;
using UnityEngine;

public class Foo : MonoBehaviour
{
    [NonSerialized] public int myValue, my{caret:Make:all:fields:serialized}Value2, myValue3;
}
