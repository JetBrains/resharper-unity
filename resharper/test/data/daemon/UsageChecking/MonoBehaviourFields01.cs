using System;
using UnityEngine;

public class A : MonoBehaviour
{
    // Warning about value never used
    public string ImplicitlyAssignedField;

    public string ImplicitlyAssignedAndUsedField;

    private string UnusedField;


    public void OnDestroy()
    {
        Console.WriteLine(ImplicitlyAssignedAndUsedField);
    }
}
