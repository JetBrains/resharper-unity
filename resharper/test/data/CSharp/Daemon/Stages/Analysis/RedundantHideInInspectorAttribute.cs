using System;
using UnityEngine;

public class Test : MonoBehaviour
{
    [HideInInspector] private int Redundant1;
    [HideInInspector] [NonSerialized] private int NotRedundant2;
    [HideInInspector] [SerializeField] private static int Redundant3;
    [HideInInspector] [SerializeField] private const int Redundant3 = 42;
    [HideInInspector] [SerializeField] private readonly int Redundant3 = 42;
}

public class Boring
{
    [HideInInspector] public int Redundant1;
    [HideInInspector] private int Redundant2;
    [HideInInspector] private static int Redundant3;
    [HideInInspector] [SerializeField] private const int Redundant4 = 42;
    [HideInInspector] [SerializeField] private readonly int Redundant3 = 42;
}
