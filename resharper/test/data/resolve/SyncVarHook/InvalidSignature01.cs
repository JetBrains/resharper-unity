using System;
using UnityEngine;
using UnityEngine.Networking;

public class A : NetworkBehaviour
{
    [SyncVar(hook = "RequiresMatchingParameterType")]
    public int IntValue;

    public void RequiresMatchingParameterType(string newValue)
    {
    }

    [SyncVar(hook = "RequiresSingleParameter")]
    public int IntValue2;

    public void RequiresSingleParameter(int newValue, string whatever)
    {
    }

    [SyncVar(hook = "RequiresVoidReturnType")]
    public int IntValue3;

    public string RequiresVoidReturnType(int newValue)
    {
        return "Hello world";
    }
}
