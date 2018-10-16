using System;
using UnityEngine;
using UnityEngine.Networking;

public class A : NetworkBehaviour
{
    [SyncVar(hook = "OnIntValueChanged")]
    public int IntValue;

    public void OnIntValueChanged(int newValue)
    {
    }

    [SyncVar(hook = "OnStringValueChanged")]
    public string StringValue;

    private static void OnStringValueChanged(string newValue)
    {
    }

    [SyncVar(hook = "OnStructValueChanged")]
    public MyStruct StructValue;

    private static void OnStructValueChanged(MyStruct newValue)
    {
    }

    public struct MyStruct
    {
        string s;
        int i;
    }

    [SyncVar(hook = "NoSuchMethod")]
    public int Value;
}
