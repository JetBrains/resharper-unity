using System;
using UnityEngine;
using UnityEditor;

[assembly: HideInInspector]

[HideInInspector]
public class Foo : MonoBehaviour
{
    [HideInInspector]
    public Foo()
    {
    }

    [HideInInspector]
    public string Field;

    [HideInInspector]
    public const string ConstField = "Hello world";

    [HideInInspector]
    public string Property { get; set; }

    [HideInInspector]
    [return: HideInInspector]
    public string Method<[HideInInspector] T>([HideInInspector] int param1)
    {
        return null;
    }

    [HideInInspector]
    public event EventHandler MyEvent;

    [field: HideInInspector]
    public event EventHandler MyEvent2;
}

[HideInInspector]
public delegate void MyEventHandler(object sender, EventArgs e);

[HideInInspector]
public struct Bar
{
}

[HideInInspector]
public enum Baz
{
    One,
    Two
}

[HideInInspector]
public interface Quux
{
}
