using System;
using UnityEngine;
using UnityEditor;

[assembly: CanEditMultipleObjects]

[CanEditMultipleObjects]
public class Foo
{
    [CanEditMultipleObjects]
    public Foo()
    {
    }

    [CanEditMultipleObjects]
    public string Field;

    [CanEditMultipleObjects]
    public const string ConstField = "Hello world";

    [CanEditMultipleObjects]
    public string Property { get; set; }

    [CanEditMultipleObjects]
    [return: CanEditMultipleObjects]
    public string Method<[CanEditMultipleObjects] T>([CanEditMultipleObjects] int param1)
    {
        return null;
    }

    [CanEditMultipleObjects]
    public event EventHandler MyEvent;

    [field: CanEditMultipleObjects]
    public event EventHandler MyEvent2;
}

[CanEditMultipleObjects]
public delegate void MyEventHandler(object sender, EventArgs e);

[CanEditMultipleObjects]
public struct Bar
{
}

[CanEditMultipleObjects]
public enum Baz
{
    One,
    Two
}

[CanEditMultipleObjects]
public interface Quux
{
}
