using System.Collections.Generic;
using UnityEngine;

namespace ListArrayFixedBufferTest
{
    public class A {}

    public class B : A
    {
        public int Value;
    }

    public class ListHolder
    {
        [SerializeReference] private List<A> _list;
    }
    public class ArrayHolder
    {
        [SerializeReference] private A[] _array;
    }
}