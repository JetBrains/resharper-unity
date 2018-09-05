using System;
using UnityEngine;

public class A : MonoBehaviour
{
    // Assigned but never used
    public string implicitlyAssignedField;
    public string implicitlyAssignedMultiField1, implicitlyAssignedMultiField2;
    [SerializeField] private int implicitlyAssignedPrivateField;

    // Assigned + used - no warning
    public string implicitlyAssignedAndUsedField;

    // Not serialized by Unity
    public const string UnusedConst = "hello";
    private const string UnusedPrivateConst = "hello";
    [SerializeField] private const string UnusedPrivateConst2 = "hello";
    private string myUnusedField;
    public readonly string UnusedReadonlyField;
    [NonSerialized] public string ExplicitlyUnusedField;
    [NonSerialized, SerializeField] public string ExplicitlyUnusedField2;
    [NonSerialized, SerializeField] private string myExplicitlyUnusedField3;
    public static string UnusedStaticField;
    [SerializeField] private static string ourUnusedPrivateStaticField;

    public void OnDestroy()
    {
        Console.WriteLine(implicitlyAssignedAndUsedField);
    }
}
