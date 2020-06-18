// ${RUN:2}
using System;
using UnityEngine;
using UnityEditor;

[assembly: SerializeField]

[SerializeFie{caret}ld]
public class Foo
{
    [CustomEditor(typeof(Material))]
    private int myField;
}

[HideInInspector]
public delegate void MyEventHandler(object sender, EventArgs e);
