using System;
using UnityEngine;

namespace WrongSerializeReferenceAttributes
{
    public class A //Shouldn't have any serialize references
    {
    }

    public class ReferenceHolderForAClass
    {
        [SerializeReference] public static A StaticField;
        [SerializeReference] private const A ConstField = null;
        [NonSerialized] [SerializeReference] public A NonSerializedField = null;
    }

    public class B //Should have 2 refs
    {
    }

    public class ReferenceHolderForBClass
    {
        [SerializeReference] public static B StaticField;
        [SerializeReference] private const B ConstField = null;
        [NonSerialized] [SerializeReference] public B NonSerializedField = null;
        [SerializeReference] public B PublicField;
        [SerializeReference] private B PrivateField;
    }
}