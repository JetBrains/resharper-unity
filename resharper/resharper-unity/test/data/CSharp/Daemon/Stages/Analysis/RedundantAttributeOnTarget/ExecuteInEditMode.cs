using System;
using UnityEngine;
using UnityEditor;

[assembly: ExecuteInEditMode]

[ExecuteInEditMode]
public class Foo
{
    [ExecuteInEditMode]
    public Foo()
    {
    }

    [ExecuteInEditMode]
    public string Field;

    [ExecuteInEditMode]
    public const string ConstField = "Hello world";

    [ExecuteInEditMode]
    public string Property { get; set; }

    [ExecuteInEditMode]
    [return: ExecuteInEditMode]
    public string Method<[ExecuteInEditMode] T>([ExecuteInEditMode] int param1)
    {
        return null;
    }

    [ExecuteInEditMode]
    public event EventHandler MyEvent;

    [field: ExecuteInEditMode]
    public event EventHandler MyEvent2;
}

[ExecuteInEditMode]
public delegate void MyEventHandler(object sender, EventArgs e);

[ExecuteInEditMode]
public struct Bar
{
}

[ExecuteInEditMode]
public enum Baz
{
    One,
    Two
}

[ExecuteInEditMode]
public interface Quux
{
}
