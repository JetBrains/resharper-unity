using UnityEngine;
using UnityEditor;

public class TestRuntimeInitializeOnLoadMethod
{
    [RuntimeInitializeOnLoadMethod]
    public void NotStatic() { }

    [RuntimeInitializeOnLoadMethod]
    public static int WrongReturnType() { }

    [RuntimeInitializeOnLoadMethod]
    public static void WrongReturnType(int arg1, int arg2) { }

    [RuntimeInitializeOnLoadMethod]
    public static void WrongTypeParameters<T, T2, T3>() { }

    [RuntimeInitializeOnLoadMethod]
    public static void JustRight() { }

    [RuntimeInitializeOnLoadMethod]
    private static void JustRight2() { }
}
