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

// Unity 6.6 serializes dictionaries natively and Odin always has, so the two must coexist.
public class TestOdinDictionaries : SerializedMonoBehaviour
{
    // Unity serializes this natively, so it is a Unity field rather than an Odin one
    [SerializeField] private Dictionary<string, int> UnitySerializedDictionary;
    // Odin serializes a public dictionary with no attribute. Marked from 6.6 on, because the dictionary rules
    // return NonSerializedField and the Odin fallback in IsSerialisedField only runs for that status - before,
    // a dictionary resolved to Unknown and the fallback was skipped. Older Unity versions still show that gap,
    // see OdinSerialisedFields.cs.gold.
    public Dictionary<string, string> PublicDictionary;
    // Odin still handles an explicit [OdinSerialize]
    [OdinSerialize] private Dictionary<string, int> OdinSerializedDictionary;
    // A dictionary-valued dictionary is serialized by Unity too
    [SerializeField] private Dictionary<string, Dictionary<string, int>> NestedDictionary;
    // Not serialized by Unity or Odin
    private Dictionary<string, string> PrivateDictionary;
}
