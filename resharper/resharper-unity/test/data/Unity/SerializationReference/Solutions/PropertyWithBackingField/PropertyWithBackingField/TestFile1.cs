using System;
using UnityEngine;

namespace PropertyWithBackingField
{
    public class A //Shouldn't have any serialize references
    {
    }

    public class ReferenceHolderForA
    {
        [field: SerializeReference] public static A StaticProperty { get; set; }
        [field: SerializeReference] private  A ReadonlyProperty { get; }
        [field: NonSerialized] [field: SerializeReference] public A NonSerializedField { get; set; }
    }
    public class B //Should have 3 refs
    {
    }

    public class ReferenceHolderForB
    {
        [field: SerializeReference] public static B StaticProperty { get; set; }
        [field: SerializeReference] public B ReadonlyProperty { get; }
        [field: NonSerialized] [field: SerializeReference] public B NonSerializedField { get; set; }
        
        [field: SerializeReference] public B PublicProperty { get; set; }
        [field: SerializeReference] public B PublicWithPrivateSetterProperty { get; private set; }
        [field: SerializeReference] private B PrivateProperty { get; set; }
    }
}