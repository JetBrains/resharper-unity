using System;
using System.Collections.Generic;
using UnityEngine;

public interface IPayload { }

public abstract class PayloadBase { }

[Serializable]
public class Payload : PayloadBase, IPayload
{
    public int Value;
}

// Before Unity 6.6 no dictionary field is serialized, whatever the key and value types and whatever attributes
// it carries. Every dictionary below is unmarked; publicListControl is the control proving this gold is not
// simply empty.
public class A : MonoBehaviour
{
    [SerializeField] private Dictionary<string, int> simple;
    [SerializeField] private Dictionary<string, List<int>> listValue;
    [SerializeField] private Dictionary<int, GameObject> objectValue;
    [SerializeField] private Dictionary<string, Payload> serializableValue;

    [SerializeField] private Dictionary<string, Dictionary<string, int>> nestedDictionary;
    [SerializeField] private Dictionary<string, Dictionary<string, Dictionary<string, int>>> doubleNestedDictionary;
    [SerializeField] private List<Dictionary<string, int>> dictionaryInList;
    [SerializeField] private Dictionary<string, int>[] dictionaryArray;
    [SerializeField] private Dictionary<string, List<Dictionary<string, int>>> listOfDictionaryAsValue;

    [SerializeField] private Dictionary<List<int>, int> collectionKey;

    [SerializeField] private Dictionary<string, IPayload> interfaceValue;
    [SerializeField] private Dictionary<string, PayloadBase> abstractValue;

    [SerializeField, SerializeReference] private Dictionary<string, IPayload> serializeReferenceDictionary;

    public Dictionary<string, int> publicWithoutAttribute;
    private Dictionary<string, int> privateWithoutAttribute;
    [SerializeField] private readonly Dictionary<string, int> readonlyWithAttribute;

    // A public List<T> is serialized at every version - the control proving this gold is not simply empty
    public List<int> publicListControl;
}
