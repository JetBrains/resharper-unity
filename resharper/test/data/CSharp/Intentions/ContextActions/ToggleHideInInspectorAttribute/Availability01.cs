using System;
using UnityEngine;

public class Foo : MonoBehaviour
{
    public int my{on}Value;
    private int my{off}Value2;
    [SerializeField] private int my{on}Value3;
    [NonSerialized] public int my{off}Value4;
    // Valid. NonSerialized wins - this is not serialized
    [SerializeField] [NonSerialized] private int my{off}Value5;
    [SerializeField] [NonSerialized] public int my{off}Value6;

    public int my{on:Annotate:field:'myValue7':with:'HideInInspector':attribute}Value7, my{on:Annotate:field:'myValue8':with:'HideInInspector':attribute}Value8, my{on:Annotate:field:'myValue9':with:'HideInInspector':attribute}Value9;

    public int my{on:Annotate:all:fields:with:'HideInInspector':attribute}value10, my{on:Annotate:all:fields:with:'HideInInspector':attribute}Value10;

    pub{off}lic sta{off}tic i{off}nt my{off}Value11;

    [HideInInspector] public string my{on:Remove:'HideInInspector':attribute}Value12;
    [HideInInspector] public string my{on:Remove:'HideInInspector':attribute:from:'myValue13'}Value13, my{on:Remove:'HideInInspector':attribute:from:'myValue14'}Value14;
    [HideInInspector] public string my{on:Remove:'HideInInspector':attribute:from:all:fields}Value15, my{on:Remove:'HideInInspector':attribute:from:all:fields}Value16;
}
