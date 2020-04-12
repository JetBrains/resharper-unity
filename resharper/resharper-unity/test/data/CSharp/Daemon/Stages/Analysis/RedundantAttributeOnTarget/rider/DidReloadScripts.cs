using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

[assembly: DidReloadScripts]

[DidReloadScripts]
public class Foo
{
    [DidReloadScripts]
    public Foo()
    {
    }

    [DidReloadScripts]
    public string Field;

    [DidReloadScripts]
    public const string ConstField = "Hello world";

    [DidReloadScripts]
    public string Property { get; set; }

    [DidReloadScripts]
    public static void OnDidReloadScripts()
    {
    }

    [return: DidReloadScripts]
    public string Method<[DidReloadScripts] T>([DidReloadScripts] int param1)
    {
        return null;
    }

    [DidReloadScripts]
    public event EventHandler MyEvent;

    [field: DidReloadScripts]
    public event EventHandler MyEvent2;
}

[DidReloadScripts]
public delegate void MyEventHandler(object sender, EventArgs e);

[DidReloadScripts]
public struct Bar
{
}

[DidReloadScripts]
public enum Baz
{
    One,
    Two
}

[DidReloadScripts]
public interface Quux
{
}
