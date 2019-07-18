using System;
using UnityEngine;

public class Foo : MonoBehaviour
{
    public int my{on:Add:'Header'}Value;
    private int my{off}Value2;
    [SerializeField] private int my{on}Value3;
    [NonSerialized] public int my{off}Value4;
    // Valid. NonSerialized wins - this is not serialized
    [SerializeField] [NonSerialized] private int my{off}Value5;
    [SerializeField] [NonSerialized] public int my{off}Value6;
    
    public int my{on:Add:'Header':before:all:fields}value10, my{on:Add:'Header':before:all:fields}Value10;
    public int my{on:Add:'Header':before:'myvalue20'}value20, my{on:Add:'Header':before:'myValue20'}Value20;

    pub{off}lic sta{off}tic i{off}nt my{off}Value11;
}
