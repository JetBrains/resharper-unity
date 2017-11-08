using UnityEngine;
using UnityEditor;

public class TestInitializeOnLoadMethod
{
    [InitializeOnLoadMethod]
    public void NotStatic() { }

    [InitializeOnLoadMethod]
    public static int WrongReturnType() { }

    [InitializeOnLoadMethod]
    public static void WrongReturnType(int arg1, int arg2) { }

    [InitializeOnLoadMethod]
    public static void WrongTypeParameters<T>() { }

    [InitializeOnLoadMethod]
    public static void JustRight() { }

    [InitializeOnLoadMethod]
    private static void JustRight2() { }
}

