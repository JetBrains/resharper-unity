using System;
using UnityEngine;
using UnityEditor;

[assembly: DrawGizmo]

[DrawGizmo]
public class Foo
{
    [DrawGizmo]
    public Foo()
    {
    }

    [DrawGizmo]
    public string Field;

    [DrawGizmo]
    public const string ConstField = "Hello world";

    [DrawGizmo]
    public string Property { get; set; }

    [DrawGizmo]
    [return: DrawGizmo]
    public string Method<[DrawGizmo] T>([DrawGizmo] int param1)
    {
        return null;
    }

    [DrawGizmo]
    public event EventHandler MyEvent;

    [field: DrawGizmo]
    public event EventHandler MyEvent2;
}

[DrawGizmo]
public delegate void MyEventHandler(object sender, EventArgs e);

[DrawGizmo]
public struct Bar
{
}

[DrawGizmo]
public enum Baz
{
    One,
    Two
}

[DrawGizmo]
public interface Quux
{
}
