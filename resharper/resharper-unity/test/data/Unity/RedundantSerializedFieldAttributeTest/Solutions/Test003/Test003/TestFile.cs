using AssemblyWithoutSerializeRef;
using UnityEngine;

namespace Test003
{
    public class B : A
    {
        [SerializeField] private int x; // This attribute is greyed out.
    }
}