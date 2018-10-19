using System;
using UnityEngine;
using UnityEditor;

[assembly: AddComponentMenu]

[AddComponentMenu]
public class Foo
{
    [AddComponentMenu]
    public Foo()
    {
    }

    [AddComponentMenu]
    public string Field;

    [AddComponentMenu]
    public const string ConstField = "Hello world";

    [AddComponentMenu]
    public string Property { get; set; }

    [AddComponentMenu]
    [return: AddComponentMenu]
    public string Method<[AddComponentMenu] T>([AddComponentMenu] int param1)
    {
        return null;
    }

    [AddComponentMenu]
    public event EventHandler MyEvent;

    [field: AddComponentMenu]
    public event EventHandler MyEvent2;
}

[AddComponentMenu]
public delegate void MyEventHandler(object sender, EventArgs e);

[AddComponentMenu]
public struct Bar
{
}

[AddComponentMenu]
public enum Baz
{
    One,
    Two
}

[AddComponentMenu]
public interface Quux
{
}
