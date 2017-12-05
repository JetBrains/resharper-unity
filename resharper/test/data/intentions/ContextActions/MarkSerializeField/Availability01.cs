using System;
using UnityEngine;

public class Foo : MonoBehaviour
{
    private int my{on}Value1;
    [NonSerialized] private int my{off}Value2;
    [SerializeField] private int my{off}Value3;
    public int my{off}Value4;
    protected int my{on}Value5;
    internal int my{on}Value6;

    public static int my{off}Value7;
    private static int my{off}Value8;
}
