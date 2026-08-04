using System;
using System.Collections.Generic;
using UnityEngine;

public enum Mode { A, B }

public enum BigMode : long { A, B }

public interface IPayload { }

public abstract class PayloadBase { }

[Serializable]
public class Payload : PayloadBase, IPayload
{
    public int Value;
}

public class A : MonoBehaviour
{
    // Serialized
    [SerializeField] private Dictionary<string, int> simple;
    [SerializeField] private Dictionary<string, List<int>> listValue;
    [SerializeField] private Dictionary<int, GameObject> objectValue;
    [SerializeField] private Dictionary<string, Payload> serializableValue;

    // Serialized: a dictionary-valued dictionary works at any depth
    [SerializeField] private Dictionary<string, Dictionary<string, int>> nestedDictionary;
    [SerializeField] private Dictionary<string, Dictionary<string, Dictionary<string, int>>> doubleNestedDictionary;

    // Not serialized: collection-of-dictionary shapes (UAC1009)
    [SerializeField] private List<Dictionary<string, int>> dictionaryInList;
    [SerializeField] private Dictionary<string, int>[] dictionaryArray;

    // Serialized: an unsupported value does not stop the field being serialized, only the values are dropped
    [SerializeField] private Dictionary<string, List<Dictionary<string, int>>> listOfDictionaryAsValue;
    [SerializeField] private Dictionary<string, object> objectTypeValue;

    // Serialized key types. long is undocumented but round-trips a full 64-bit value
    [SerializeField] private Dictionary<Mode, int> enumKey;
    [SerializeField] private Dictionary<Vector3, int> builtinStructKey;
    [SerializeField] private Dictionary<Payload, int> serializableClassKey;
    [SerializeField] private Dictionary<long, int> longKey;

    // Not serialized: collection key (UAC1013)
    [SerializeField] private Dictionary<List<int>, int> collectionKey;

    // Not serialized: enums must be 32 bits or smaller (UAC1011). The rule is general, not dictionary-specific
    [SerializeField] private Dictionary<BigMode, int> bigEnumKey;
    [SerializeField] private Dictionary<string, BigMode> bigEnumValue;
    [SerializeField] private BigMode plainBigEnum;
    [SerializeField] private List<BigMode> bigEnumList;
    [SerializeField] private BigMode[] bigEnumArray;

    // Serialized: controls proving the rule above is about enum width
    [SerializeField] private Mode plainSmallEnum;
    [SerializeField] private List<Mode> smallEnumList;

    // Not serialized: interface or abstract key/value (UAC1012)
    [SerializeField] private Dictionary<string, IPayload> interfaceValue;
    [SerializeField] private Dictionary<string, PayloadBase> abstractValue;

    // Not serialized: [SerializeReference] is not honoured for dictionaries (UAC1014), and does not substitute
    // for [SerializeField] when used on its own
    [SerializeField, SerializeReference] private Dictionary<string, IPayload> serializeReferenceDictionary;
    [SerializeReference] private Dictionary<string, IPayload> serializeReferenceOnly;
    [SerializeReference] private Dictionary<string, int> serializeReferenceOnlyValidTypes;

    // Not serialized: serialization is opt-in, a public field is not enough (UAC1015)
    public Dictionary<string, int> publicWithoutAttribute;
    private Dictionary<string, int> privateWithoutAttribute;
    [SerializeField] private readonly Dictionary<string, int> readonlyWithAttribute;

    // Serialized: contrast case - a public List<T> IS serialized without [SerializeField]
    public List<int> publicListControl;

    // [field: SerializeField] serializes the backing field of an auto property, so the same dictionary rules
    // apply. Only a writable, non-static, attributed property qualifies.
    [field: SerializeField] public Dictionary<string, int> SerializedDictionaryProperty { get; set; }
    [field: SerializeField] public Dictionary<List<int>, int> SerializedBadKeyProperty { get; set; }
    [field: SerializeField] public Dictionary<string, int> GetOnlyDictionaryProperty { get; }
    public Dictionary<string, int> PlainDictionaryProperty { get; set; }
}

// Unity tolerates a type parameter in T[] and List<T>, but not as a dictionary key or value (UAC1016).
public class G<T> : MonoBehaviour
{
    [SerializeField] private T[] arrayOfT;
    [SerializeField] private List<T> listOfT;

    [SerializeField] private Dictionary<string, T> dictionaryValueOfT;
    [SerializeField] private Dictionary<T, int> dictionaryKeyOfT;
}
