using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class A : MonoBehaviour
{
    // All serialised by Unity - gutter icons
    public string ImplicitlyAssignedField;
    public string ImplicitlyAssignedMultiField1, ImplicitlyAssignedMultiField2;
    [SerializeField] private int myImplicitlyAssignedPrivateField;
    public Vector2 ImplicitlyAssignedBuiltinType;
    public Vector2Int ImplicitlyAssignedBuiltinType2;
    public Vector3 ImplicitlyAssignedBuiltinType3;
    public Vector3Int ImplicitlyAssignedBuiltinType4;
    public Vector4 ImplicitlyAssignedBuiltinType5;
    public Rect ImplicitlyAssignedBuiltinType6;
    public RectInt ImplicitlyAssignedBuiltinType7;
    public RectOffset ImplicitlyAssignedBuiltinType8;
    public Quaternion ImplicitlyAssignedBuiltinType9;
    public Matrix4x4 ImplicitlyAssignedBuiltinType10;
    public Color ImplicitlyAssignedBuiltinType11;
    public Color32 ImplicitlyAssignedBuiltinType12;
    public LayerMask ImplicitlyAssignedBuiltinType13;
    public Bounds ImplicitlyAssignedBuiltinType14;
    public BoundsInt ImplicitlyAssignedBuiltinType15;
    public AnimationCurve ImplicitlyAssignedBuiltinType16;
    public Gradient ImplicitlyAssignedBuiltinType17;
    public GUIStyle ImplicitlyAssignedBuiltinType18;
    public List<string> ImplicitlyAssignedList;
    public string[] ImplicitlyAssignedArray;
    public SerialisableClass ImplicitlyAssignedCustomSerialisableClass;
    public SerialisableStruct ImplicitlyAssignedCustomSerialisableStruct;
    public SerialisableClass[] ImplicitlyAssignedCustomSerialisableClassArray;
    public SerialisableStruct[] ImplicitlyAssignedCustomSerialisableStructArray;

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
    public Version NotSerialisedStruct;
    public Dictionary<string, string> NotSerialisedDictionary;
    public NotSerialisableClass NotSerialisedClass;
    public List<Version> NotSerialisedList;
    public string[,] NotSerialisedMultidimensionalArray;
    public string[][] NotSerialisedJaggedArray;

    // Unity function
    private void OnDestroy()
    {
    }

    // Not a Unity function
    private void NotMessage()
    {
    }

    // Unity message as coroutine
    private IEnumerator Start()
    {
        return null;
    }

    // Optional parameter
    private void OnCollisionStay()
    {
    }
}

[InitializeOnLoad]
public class Startup
{
    static Startup()
    {
        Debug.Log("Up and running");
    }

    public Startup()
    {
        // Not used
    }
}

class MyClass
{
    [RuntimeInitializeOnLoadMethod]
    static void OnRuntimeMethodLoad()
    {
        Debug.Log("After scene is loaded and game is running");
    }

    [RuntimeInitializeOnLoadMethod]
    public static void OnSecondRuntimeMethodLoad()
    {
        Debug.Log("SecondMethod After scene is loaded and game is running.");
    }

    [RuntimeInitializeOnLoadMethod]
    public void NotAppliedToInstanceMethods()
    {
    }

    [InitializeOnLoadMethod]
    private static void OnProjectLoadedInEditor()
    {
        Debug.Log("Project loaded in Unity Editor");
    }

    [InitializeOnLoadMethod]
    public static void OnProjectLoadedInEditor2()
    {
        Debug.Log("Project loaded in Unity Editor");
    }

    [InitializeOnLoadMethod]
    public void NotAppliedToInstanceMethod()
    {
        Debug.Log("Project loaded in Unity Editor");
    }
}

[Serializable]
public class SerialisableClass
{
    // All serialised by Unity - gutter icons
    public string ImplicitlyAssignedField;
    public string ImplicitlyAssignedMultiField1, ImplicitlyAssignedMultiField2;
    [SerializeField] private int myImplicitlyAssignedPrivateField;
    public Vector2 ImplicitlyAssignedBuiltinType;
    public Vector2Int ImplicitlyAssignedBuiltinType2;
    public Vector3 ImplicitlyAssignedBuiltinType3;
    public Vector3Int ImplicitlyAssignedBuiltinType4;
    public Vector4 ImplicitlyAssignedBuiltinType5;
    public Rect ImplicitlyAssignedBuiltinType6;
    public RectInt ImplicitlyAssignedBuiltinType7;
    public RectOffset ImplicitlyAssignedBuiltinType8;
    public Quaternion ImplicitlyAssignedBuiltinType9;
    public Matrix4x4 ImplicitlyAssignedBuiltinType10;
    public Color ImplicitlyAssignedBuiltinType11;
    public Color32 ImplicitlyAssignedBuiltinType12;
    public LayerMask ImplicitlyAssignedBuiltinType13;
    public Bounds ImplicitlyAssignedBuiltinType14;
    public BoundsInt ImplicitlyAssignedBuiltinType15;
    public AnimationCurve ImplicitlyAssignedBuiltinType16;
    public Gradient ImplicitlyAssignedBuiltinType17;
    public GUIStyle ImplicitlyAssignedBuiltinType18;
    public List<string> ImplicitlyAssignedList;
    public string[] ImplicitlyAssignedArray;
    public SerialisableClass ImplicitlyAssignedCustomSerialisableClass;
    public SerialisableStruct ImplicitlyAssignedCustomSerialisableStruct;
    public SerialisableClass[] ImplicitlyAssignedCustomSerialisableClassArray;
    public SerialisableStruct[] ImplicitlyAssignedCustomSerialisableStructArray;

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
    public Version NotSerialisedStruct;
    public Dictionary<string, string> NotSerialisedDictionary;
    public NotSerialisableClass NotSerialisedClass;
    public List<Version> NotSerialisedList;
    public string[,] NotSerialisedMultidimensionalArray;
    public string[][] NotSerialisedJaggedArray;
}

[Serializable]
public struct SerialisableStruct
{
    // All serialised by Unity - gutter icons
    public string ImplicitlyAssignedField;
    public string ImplicitlyAssignedMultiField1, ImplicitlyAssignedMultiField2;
    [SerializeField] private int myImplicitlyAssignedPrivateField;
    public Vector2 ImplicitlyAssignedBuiltinType;
    public Vector2Int ImplicitlyAssignedBuiltinType2;
    public Vector3 ImplicitlyAssignedBuiltinType3;
    public Vector3Int ImplicitlyAssignedBuiltinType4;
    public Vector4 ImplicitlyAssignedBuiltinType5;
    public Rect ImplicitlyAssignedBuiltinType6;
    public RectInt ImplicitlyAssignedBuiltinType7;
    public RectOffset ImplicitlyAssignedBuiltinType8;
    public Quaternion ImplicitlyAssignedBuiltinType9;
    public Matrix4x4 ImplicitlyAssignedBuiltinType10;
    public Color ImplicitlyAssignedBuiltinType11;
    public Color32 ImplicitlyAssignedBuiltinType12;
    public LayerMask ImplicitlyAssignedBuiltinType13;
    public Bounds ImplicitlyAssignedBuiltinType14;
    public BoundsInt ImplicitlyAssignedBuiltinType15;
    public AnimationCurve ImplicitlyAssignedBuiltinType16;
    public Gradient ImplicitlyAssignedBuiltinType17;
    public GUIStyle ImplicitlyAssignedBuiltinType18;
    public List<string> ImplicitlyAssignedList;
    public string[] ImplicitlyAssignedArray;
    public SerialisableClass ImplicitlyAssignedCustomSerialisableClass;
    public SerialisableStruct ImplicitlyAssignedCustomSerialisableStruct;
    public SerialisableClass[] ImplicitlyAssignedCustomSerialisableClassArray;
    public SerialisableStruct[] ImplicitlyAssignedCustomSerialisableStructArray;

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
}

public class NotSerialisableClass
{
    public string NotSerialised1;
    [SerializeField] public string NotSerialised2;
}

struct NotSerialisableStruct
{
    public string NotSerialised1;
    [SerializeField] public string NotSerialised2;
}

[Serializable]
static class NotSerialisableStaticClass
{
    public static string NotSerialised1;
    [SerializeField] public static string NotSerialised2;
}
