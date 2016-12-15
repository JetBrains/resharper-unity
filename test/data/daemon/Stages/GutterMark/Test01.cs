using System;
using UnityEngine;
using UnityEditor;

public class A : MonoBehaviour
{
    public object unityField;
    private object notUnityField;

    public object unityField1, unityField2;

    [NonSerialized]
    public object notUnityField2;

    [SerializeField]
    private object unityField3;

    [SerializeField]
    private object unityField4, unityField5;

    // Unity function
    private void OnDestroy()
    {
    }

    // Not a Unity function
    private void NotMessage()
    {
    }
}

[InitializeOnLoad]
public class Startup
{
    static Startup()
    {
        Debug.Log("Up and running");
    }

    public Startup()
    {
        // Not used
    }
}

class MyClass
{
    [RuntimeInitializeOnLoadMethod]
    static void OnRuntimeMethodLoad()
    {
        Debug.Log("After scene is loaded and game is running");
    }

    [RuntimeInitializeOnLoadMethod]
    public static void OnSecondRuntimeMethodLoad()
    {
        Debug.Log("SecondMethod After scene is loaded and game is running.");
    }

    [RuntimeInitializeOnLoadMethod]
    public void NotAppliedToInstanceMethods()
    {
    }

    [InitializeOnLoadMethod]
    private static void OnProjectLoadedInEditor()
    {
        Debug.Log("Project loaded in Unity Editor");
    }

    [InitializeOnLoadMethod]
    public static void OnProjectLoadedInEditor2()
    {
        Debug.Log("Project loaded in Unity Editor");
    }

    [InitializeOnLoadMethod]
    public void NotAppliedToInstanceMethod()
    {
        Debug.Log("Project loaded in Unity Editor");
    }
}
