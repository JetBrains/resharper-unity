using System;
using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField] public int NotRedundant1;  // OK to be explicit
    [SerializeField] private int NotRedundant2;
    [SerializeField] [NonSerialized] public int Redundant1;
    [SerializeField] [NonSerialized] private int Redundant1;
    [SerializeField] private readonly int ReadonlyFieldsAreNotSerialized;
    [SerializeField] private const int ConstFieldsAreNotSerialized;
    [SerializeField] private static int StaticFieldsAreNotSerialized;
}
