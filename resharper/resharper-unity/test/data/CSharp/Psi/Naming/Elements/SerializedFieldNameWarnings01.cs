using System;
using UnityEngine;

public class MyMonoBehaviour : MonoBehaviour
{
    public float serializedField01;
    public float SerializedField02;

    [SerializeField] private float mySerializedField03;

    public static float NonSerializedField04;
    [NonSerialized] public float NonSerializedField05;
    private float NonSerializedField06;
}

[Serializable]
public class SerializedClass
{
    public float serializedField01;
    public float SerializedField02;

    [SerializeField] private float mySerializedField03;

    public static float NonSerializedField04;
    [NonSerialized] public float NonSerializedField05;
    private float NonSerializedField06;
}

public class NotSerialized
{
    public float serializedField01;
    public float SerializedField02;

    [SerializeField] private float mySerializedField03;

    public static float NonSerializedField04;
    [NonSerialized] public float NonSerializedField05;
    private float NonSerializedField06;
}
