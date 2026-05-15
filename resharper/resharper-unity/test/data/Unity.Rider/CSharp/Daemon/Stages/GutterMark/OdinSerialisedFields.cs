using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

// Stubs for Sirenix.Odin types. Tests run without a real Odin reference.
namespace Sirenix.OdinInspector
{
    public abstract class SerializedMonoBehaviour : MonoBehaviour { }
    public abstract class SerializedScriptableObject : ScriptableObject { }
}

namespace Sirenix.Serialization
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class OdinSerializeAttribute : Attribute { }
}

// SerializedScriptableObject - matches the user's confirmed repro case.
public class TestSerializedScriptableObject : SerializedScriptableObject
{
    // Unity-serializable type + public => Unity inspector gutter
    public int PublicField;
    // [SerializeField] forces Unity serialization on private field
    [SerializeField] private int PrivateSerializeField;
    // Bug fix: private field with no attribute must NOT be marked serialized
    private int PrivateField;
    // [OdinSerialize] opts a private field into Odin serialization
    [OdinSerialize] private int OdinSerializedPrivate;
    // [NonSerialized] wins over public when no [OdinSerialize]
    [NonSerialized] public int NonSerializedPublic;
    // [OdinSerialize] overrules [NonSerialized] (Odin behavior)
    [NonSerialized, OdinSerialize] public int NonSerializedOdinSerialize;
    // Public field of a type Unity can't serialize => Odin handles it
    public Dictionary<string, string> PublicDictionary;
    // [OdinSerialize] on a private field of a Unity-non-serializable type
    [OdinSerialize] private Dictionary<string, int> OdinSerializedDictionary;
    // Private field of a non-Unity-serializable type without attributes => not serialized
    private Dictionary<string, string> PrivateDictionary;
}

// SerializedMonoBehaviour - secondary repro case.
public class TestSerializedMonoBehaviour : SerializedMonoBehaviour
{
    public int PublicField;
    [SerializeField] private int PrivateSerializeField;
    private int PrivateField;
    [OdinSerialize] private int OdinSerializedPrivate;
    [NonSerialized] public int NonSerializedPublic;
    public Dictionary<string, string> PublicDictionary;
}

// Plain MonoBehaviour - control case: [OdinSerialize] does nothing outside Odin-derived types.
public class TestPlainMonoBehaviour : MonoBehaviour
{
    public int PublicField;
    private int PrivateField;
    [OdinSerialize] private int OdinSerializedPrivate;
}
