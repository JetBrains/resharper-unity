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
    public static void JustRight() { }

    [InitializeOnLoadMethod]
    private static void JustRight2() { }
}

public class TestRuntimeInitializeOnLoadMethod
{
    [RuntimeInitializeOnLoadMethod]
    public void NotStatic() { }

    [RuntimeInitializeOnLoadMethod]
    public static int WrongReturnType() { }

    [RuntimeInitializeOnLoadMethod]
    public static void WrongReturnType(int arg1, int arg2) { }

    [RuntimeInitializeOnLoadMethod]
    public static void JustRight() { }

    [RuntimeInitializeOnLoadMethod]
    private static void JustRight2() { }
}
