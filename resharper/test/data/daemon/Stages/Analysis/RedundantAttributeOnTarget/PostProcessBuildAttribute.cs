using System;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;

[assembly: PostProcessBuild]

[PostProcessBuild]
public class Foo
{
    [PostProcessBuild]
    public Foo()
    {
    }

    [PostProcessBuild]
    public string Field;

    [PostProcessBuild]
    public const string ConstField = "Hello world";

    [PostProcessBuild]
    public string Property { get; set; }

    [PostProcessBuild]
    public static void DoPostProcessBuilder(BuildTarget target, string pathToBuildProject)
    {
    }

    [return: PostProcessBuild]
    public string Method<[PostProcessBuild] T>([PostProcessBuild] int param1)
    {
        return null;
    }

    [PostProcessBuild]
    public event EventHandler MyEvent;

    [field: PostProcessBuild]
    public event EventHandler MyEvent2;
}

[PostProcessBuild]
public delegate void MyEventHandler(object sender, EventArgs e);

[PostProcessBuild]
public struct Bar
{
}

[PostProcessBuild]
public enum Baz
{
    One,
    Two
}

[PostProcessBuild]
public interface Quux
{
}
