using System;
using UnityEngine;
using UnityEditor;

[assembly: ImageEffectOpaque]

[ImageEffectOpaque]
public class Foo
{
    [ImageEffectOpaque]
    public Foo()
    {
    }

    [ImageEffectOpaque]
    public string Field;

    [ImageEffectOpaque]
    public const string ConstField = "Hello world";

    [ImageEffectOpaque]
    public string Property { get; set; }

    [ImageEffectOpaque]
    [return: ImageEffectOpaque]
    public string Method<[ImageEffectOpaque] T>([ImageEffectOpaque] int param1)
    {
        return null;
    }

    [ImageEffectOpaque]
    public event EventHandler MyEvent;

    [field: ImageEffectOpaque]
    public event EventHandler MyEvent2;
}

[ImageEffectOpaque]
public delegate void MyEventHandler(object sender, EventArgs e);

[ImageEffectOpaque]
public struct Bar
{
}

[ImageEffectOpaque]
public enum Baz
{
    One,
    Two
}

[ImageEffectOpaque]
public interface Quux
{
}
