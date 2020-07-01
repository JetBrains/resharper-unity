using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class A : MonoBehaviour
{
    public SerialisableClass1<string> value1;
    public SerialisableClass2<string, int> value2;
    public SerialisableClass2<string, SerializableClass1<string>> value3;
}

[Serializable]
public class SerialisableClass1<T>
{
    public T one;
}

[Serializable]
public class SerialisableClass2<T1, T2>
{
    public T1 one;
    public T2 two;
}

