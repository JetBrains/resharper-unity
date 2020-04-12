using System;
using UnityEngine;

public class Foo : MonoBehaviour
{
    public int my{on:Add:'HideInInspector'}Value;
    private int my{off}Value2;
    [SerializeField] private int my{on}Value3;
    [NonSerialized] public int my{off}Value4;
    // Valid. NonSerialized wins - this is not serialized
    [SerializeField] [NonSerialized] private int my{off}Value5;
    [SerializeField] [NonSerialized] public int my{off}Value6;
    
    public int my{on:Add:'HideInInspector':to:'myValue7'}Value7, my{on:Add:'HideInInspector':to:'myValue8'}Value8, my{on:Add:'HideInInspector':to:'myValue9'}Value9;

    public int my{on:Add:'HideInInspector':to:all:fields}value10, my{on:Add:'HideInInspector':to:all:fields}Value10;

    pub{off}lic sta{off}tic i{off}nt my{off}Value11;

    [HideInInspector] public string my{on:Remove:'HideInInspector'}Value12;
    [HideInInspector] public string my{on:Remove:'HideInInspector':from:'myValue13'}Value13, my{on:Remove:'HideInInspector':from:'myValue14'}Value14;
    [HideInInspector] public string my{on:Remove:'HideInInspector':from:all:fields}Value15, my{on:Remove:'HideInInspector':from:all:fields}Value16;
}
