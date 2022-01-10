using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

[assembly: PostProcessScene]

[PostProcessScene]
public class Foo
{
    [PostProcessScene]
    public Foo()
    {
    }

    [PostProcessScene]
    public string Field;

    [PostProcessScene]
    public const string ConstField = "Hello world";

    [PostProcessScene]
    public string Property { get; set; }

    [PostProcessScene]
    public static void DoPostProcessScene()
    {
    }

    [return: PostProcessScene]
    public string Method<[PostProcessScene] T>([PostProcessScene] int param1)
    {
        return null;
    }

    [PostProcessScene]
    public event EventHandler MyEvent;

    [field: PostProcessScene]
    public event EventHandler MyEvent2;
}

[PostProcessScene]
public delegate void MyEventHandler(object sender, EventArgs e);

[PostProcessScene]
public struct Bar
{
}

[PostProcessScene]
public enum Baz
{
    One,
    Two
}

[PostProcessScene]
public interface Quux
{
}
