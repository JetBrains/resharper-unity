using System;
using UnityEngine;

namespace AssemblyWithoutSerializeRef
{
    public class Foo : MonoBehaviour
    {
       private A bar;
    }

    [Serializable]
    public abstract class A
    {
    }
}