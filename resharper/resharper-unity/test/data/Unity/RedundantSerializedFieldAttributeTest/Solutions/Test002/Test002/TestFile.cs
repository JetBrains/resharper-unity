using AssemblyWithSerializedRef;
using UnityEngine;

namespace Test002
{
    public class B : A
    {
        [SerializeField] private int x; // SerializeField - shouldn't be marked as redundant
    }
}