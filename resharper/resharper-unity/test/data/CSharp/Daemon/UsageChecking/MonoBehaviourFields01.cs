using System;
using System.Collections.Generic;
using UnityEngine;

public class A : MonoBehaviour
{
    // Assigned but never used
    public string implicitlyAssignedField;
    public string implicitlyAssignedMultiField1, implicitlyAssignedMultiField2;
    [SerializeField] private int implicitlyAssignedPrivateField;
    [SerializeReference] private string implicitlyAssignedPrviateField2;

    // Assigned + used - no warning
    public string implicitlyAssignedAndUsedField;

    public List<string> implicitlyAssignedList;

    [SerializeReference] private Track implicityAssignedAbstractField;
    private Track myUnusedAbstractField;

    // Not serialized by Unity
    public Action UnusedAction;
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

public class B<T> : MonoBehaviour
{
    // We don't know if T is serialisable or not. It's better to assume it is
    public T possiblySerialisedField;
    [SerializeField] private T possiblySerialisedField2;

    public T[] possiblySerialisedArray;
    [SerializeField] private T[] possiblySerialisedArray2;

    public List<T> possiblySerialisedList;
    [SerializeField] private List<T> possiblySerialisedList2;
}

[Serializable]
public abstract class Track
{
    public string name;
}
