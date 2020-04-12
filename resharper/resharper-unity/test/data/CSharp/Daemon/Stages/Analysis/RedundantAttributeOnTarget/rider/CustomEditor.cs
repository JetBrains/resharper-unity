using System;
using UnityEngine;
using UnityEditor;

[assembly: CustomEditor]

[CustomEditor]
public class Foo
{
    [CustomEditor]
    public Foo()
    {
    }

    [CustomEditor]
    public string Field;

    [CustomEditor]
    public const string ConstField = "Hello world";

    [CustomEditor]
    public string Property { get; set; }

    [CustomEditor]
    [return: CustomEditor]
    public string Method<[CustomEditor] T>([CustomEditor] int param1)
    {
        return null;
    }

    [CustomEditor]
    public event EventHandler MyEvent;

    [field: CustomEditor]
    public event EventHandler MyEvent2;
}

[CustomEditor]
public delegate void MyEventHandler(object sender, EventArgs e);

[CustomEditor]
public struct Bar
{
}

[CustomEditor]
public enum Baz
{
    One,
    Two
}

[CustomEditor]
public interface Quux
{
}
