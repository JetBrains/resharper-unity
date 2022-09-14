using System;
using UnityEngine;
using UnityEngine.Events;

namespace Platformer.Mechanics
{
    [Serializable]
    public struct MyStruct
    {
        public UnityEvent myUnityEvent;
    }

    [Serializable]
    public struct MyStruct2
    {
        public MyStruct myStruct;
    }
    
    [Serializable]
    public struct MyStruct1
    {
        public UnityEvent myUnityEvent1;
    }
    
    [Serializable]
    public struct MyStructArray2
    {
        public MyStruct1 myStruct1;
    }

    public class Test : MonoBehaviour
    {
        public MyStruct2 myStruct2;
        public MyStructArray2[] myStructArray2;
    }
}