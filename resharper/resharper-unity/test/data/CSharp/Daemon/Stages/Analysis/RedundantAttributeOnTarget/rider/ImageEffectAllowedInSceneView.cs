using System;
using UnityEngine;
using UnityEditor;

[assembly: ImageEffectAllowedInSceneView]

[ImageEffectAllowedInSceneView]
public class Foo
{
    [ImageEffectAllowedInSceneView]
    public Foo()
    {
    }

    [ImageEffectAllowedInSceneView]
    public string Field;

    [ImageEffectAllowedInSceneView]
    public const string ConstField = "Hello world";

    [ImageEffectAllowedInSceneView]
    public string Property { get; set; }

    [ImageEffectAllowedInSceneView]
    [return: ImageEffectAllowedInSceneView]
    public string Method<[ImageEffectAllowedInSceneView] T>([ImageEffectAllowedInSceneView] int param1)
    {
        return null;
    }

    [ImageEffectAllowedInSceneView]
    public event EventHandler MyEvent;

    [field: ImageEffectAllowedInSceneView]
    public event EventHandler MyEvent2;
}

[ImageEffectAllowedInSceneView]
public delegate void MyEventHandler(object sender, EventArgs e);

[ImageEffectAllowedInSceneView]
public struct Bar
{
}

[ImageEffectAllowedInSceneView]
public enum Baz
{
    One,
    Two
}

[ImageEffectAllowedInSceneView]
public interface Quux
{
}
