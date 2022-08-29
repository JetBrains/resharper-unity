// ${RUN:2}
using System;
using UnityEngine;

[Serializable]
public struct A
{
    [Serialize{caret}Field] public unsafe fixed byte myByteBuff1[3];
    [SerializeField] private unsafe fixed byte myByteBuff2[3];
}


public class Test : MonoBehaviour
{
    public A Value1;
}
