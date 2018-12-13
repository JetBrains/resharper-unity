using System;
using UnityEngine;
using UnityEditor;

[assembly: ImageEffectTransformsToLDR]

[ImageEffectTransformsToLDR]
public class Foo
{
    [ImageEffectTransformsToLDR]
    public Foo()
    {
    }

    [ImageEffectTransformsToLDR]
    public string Field;

    [ImageEffectTransformsToLDR]
    public const string ConstField = "Hello world";

    [ImageEffectTransformsToLDR]
    public string Property { get; set; }

    [ImageEffectTransformsToLDR]
    [return: ImageEffectTransformsToLDR]
    public string Method<[ImageEffectTransformsToLDR] T>([ImageEffectTransformsToLDR] int param1)
    {
        return null;
    }

    [ImageEffectTransformsToLDR]
    public event EventHandler MyEvent;

    [field: ImageEffectTransformsToLDR]
    public event EventHandler MyEvent2;
}

[ImageEffectTransformsToLDR]
public delegate void MyEventHandler(object sender, EventArgs e);

[ImageEffectTransformsToLDR]
public struct Bar
{
}

[ImageEffectTransformsToLDR]
public enum Baz
{
    One,
    Two
}

[ImageEffectTransformsToLDR]
public interface Quux
{
}
