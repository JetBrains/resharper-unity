using System;
using UnityEngine;

namespace AssemblyWithSerializedRef
{
    public class Foo : MonoBehaviour
    {
        [SerializeReference] private A bar;
    }

    [Serializable]
    public abstract class A
    {
    }
}