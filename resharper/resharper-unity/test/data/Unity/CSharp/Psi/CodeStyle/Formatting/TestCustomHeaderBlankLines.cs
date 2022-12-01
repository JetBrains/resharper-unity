using System;
using UnityEngine;

public class CustomHeaderFormatting : MonoBehaviour
{
    [Header("Something")] [SerializeField] private int YyyyyyField; // Serialised field
    
    [Header("Something")] 
    [SerializeField] public const int OooooField = 42;
    
    [Header("Something")]
    [SerializeField] 
    public const int HhhhhField = 42;
    
    [Header("Something else")] [SerializeField]
    public string AaaaaField3;
    
    [Header("Something else")] [SerializeField] [Obsolete]
    public string AaaaaField4;
    
    [Header("Something else")] [SerializeField] [Obsolete] [Obsolete] [Obsolete]
    public string AaaaaField6;
    
    [Header("Something else")] [SerializeField // 123
    ] [Obsolete]
    public string AaaaaField5;

    [Header("Something else")] public string AaaaaField2; // Serialised field

    [SerializeField] private int ZzzzzField; // Serialised field
    
    [SerializeField] [Obsolete] private int ZzzzzField1;

    public const int OooooField = 42;
}