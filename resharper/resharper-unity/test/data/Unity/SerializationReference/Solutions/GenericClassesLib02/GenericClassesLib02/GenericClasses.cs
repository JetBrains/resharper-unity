using UnityEngine;

namespace GenericClassesLib02
{
    public class X
    {
    }

    public class OwnerClass
    {
        public class NestedClass<T_IN_NESTED>
        {
            [SerializeReference] private T_IN_NESTED T_IN_NESTED__NESTED_CLASS_FIELD;
        }
    }

    public class InheritorClass : OwnerClass.NestedClass<X>
    {
    }
}