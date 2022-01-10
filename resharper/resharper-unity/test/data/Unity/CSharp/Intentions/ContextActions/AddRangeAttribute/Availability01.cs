using System;
using UnityEngine;

public class Foo : MonoBehaviour
{
    public int my{on:Add:'Range'}Value;
    private int my{off}Value2;
    [SerializeField] private int my{on}Value3;
    [NonSerialized] public int my{off}Value4;
    // Valid. NonSerialized wins - this is not serialized
    [SerializeField] [NonSerialized] private int my{off}Value5;
    [SerializeField] [NonSerialized] public int my{off}Value6;
    
    public int my{on:Add:'Range':to:'myvalue10'}value10, my{on:Add:'Range':to:'myValue10'}Value10;

    pub{off}lic sta{off}tic i{off}nt my{off}Value11;
}
