using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

[assembly: OnOpenAsset]

[OnOpenAsset]
public class Foo
{
    [OnOpenAsset]
    public Foo()
    {
    }

    [OnOpenAsset]
    public string Field;

    [OnOpenAsset]
    public const string ConstField = "Hello world";

    [OnOpenAsset]
    public string Property { get; set; }

    [OnOpenAsset]
    public static bool DoOnOpenAsset(int instanceID, int line)
    {
        return true;
    }

    [return: OnOpenAsset]
    public string Method<[OnOpenAsset] T>([OnOpenAsset] int param1)
    {
        return null;
    }

    [OnOpenAsset]
    public event EventHandler MyEvent;

    [field: OnOpenAsset]
    public event EventHandler MyEvent2;
}

[OnOpenAsset]
public delegate void MyEventHandler(object sender, EventArgs e);

[OnOpenAsset]
public struct Bar
{
}

[OnOpenAsset]
public enum Baz
{
    One,
    Two
}

[OnOpenAsset]
public interface Quux
{
}
