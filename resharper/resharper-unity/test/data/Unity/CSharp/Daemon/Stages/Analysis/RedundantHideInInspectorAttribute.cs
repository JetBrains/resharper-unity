using System;
using UnityEngine;

public class Test : MonoBehaviour
{
    [HideInInspector] private int Redundant1;
    [HideInInspector] [NonSerialized] private int NotRedundant2;
    [HideInInspector] [SerializeField] private static int Redundant3;
    [HideInInspector] [SerializeField] private const int Redundant4 = 42;
    [HideInInspector] [SerializeField] private readonly int Redundant5 = 42;
    [field: HideInInspector, SerializeField] public string NotRedundant6 { get; set; }
    [field: HideInInspector] public string Redundant7 { get; set; }
}

public class Boring
{
    [HideInInspector] public int Redundant1;
    [HideInInspector] private int Redundant2;
    [HideInInspector] private static int Redundant3;
    [HideInInspector] [SerializeField] private const int Redundant4 = 42;
    [HideInInspector] [SerializeField] private readonly int Redundant5 = 42;
    [field: HideInInspector, SerializeField] public string GloballyRedundant6 { get; set; }
    [field: HideInInspector] public string Redundant7 { get; set; }
}
