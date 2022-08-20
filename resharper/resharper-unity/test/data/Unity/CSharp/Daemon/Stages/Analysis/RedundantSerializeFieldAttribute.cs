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

    // We can serialize the backing field of an auto property, but only if it's got a setter (or the backing field is
    // generated as readonly) and not static (or the backing field is static)
    [field: SerializeField] public string Property1 { get; set; }
    [field: SerializeField] private string Property2 { get; private set; }
    [field: SerializeField] private string Property3 { get; }
    [field: SerializeField] private string Property4 { get; init; }
    [field: SerializeField] public static string Property5 { get; set; }
}
