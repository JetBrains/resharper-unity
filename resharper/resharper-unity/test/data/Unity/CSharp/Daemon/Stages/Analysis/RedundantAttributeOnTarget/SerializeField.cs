using System;
using UnityEngine;
using UnityEditor;

[assembly: SerializeField]

[SerializeField]
public class Foo
{
    [SerializeField]
    public Foo()
    {
    }

    [SerializeField]
    public string Field;

    [SerializeField]
    public const string ConstField = "Hello world";

    [SerializeField]
    [field: SerializeField]
    public string Property { get; set; }

    [field: SerializeField]
    public string Property2 { get; private set; }

    [field: SerializeField]
    public string Property3 { get; }

    [field: SerializeField]
    public string Property4 { get; init; }

    [field: SerializeField]
    public static string Property5 { get; set; }

    [SerializeField]
    [return: SerializeField]
    public string Method<[SerializeField] T>([SerializeField] int param1)
    {
        return null;
    }

    [SerializeField]
    public event EventHandler MyEvent;

    [field: SerializeField]
    public event EventHandler MyEvent2;
}

[SerializeField]
public delegate void MyEventHandler(object sender, EventArgs e);

[SerializeField]
public struct Bar
{
}

[SerializeField]
public enum Baz
{
    One,
    Two
}

[SerializeField]
public interface Quux
{
}
