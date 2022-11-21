using UnityEngine;

namespace GenericClassesLib03
{
    public class OwnerClass<T_IN_OWNER>//: ParentClass, IMyInterface
    {
        public class NestedClassClear
        {
            [SerializeReference] private T_IN_OWNER T_IN_OWNER__NESTED_CLASS_FIELD;
        }
    }
}