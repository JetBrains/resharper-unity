using System;
using UnityEngine;

namespace RedundantFieldsWithGenerics
{
    [Serializable]
    internal class ClassWithInterfaceField
    {
        [SerializeReference] public IMyInterface MyInterface;
    }

    internal class ClassWithXAndYFields
    {
        [SerializeReference] public X X_Field_In_C_Class;
        [SerializeReference] public Y Y_Field_In_C_Class;
    }

    public class C : B<X, Y>
    {
    }

    public class A<T0_IN_A> : IMyInterface
    {
        [SerializeReference] public T0_IN_A T0_IN_A_Class_Field;
    }

    public class B<T0_IN_B__FROM_T0_IN_A, T1_IN_B> : A<T0_IN_B__FROM_T0_IN_A>
    {
        [SerializeReference] public T1_IN_B T1_IN_B_Class_Field;
    }

    public class X
    {
        [SerializeField] private int IntValue;
    }

    public class Y
    {
    }
    
    public class L
    {
        [SerializeField] private int IntValue;
    }

    public interface IMyInterface
    {
    }
}