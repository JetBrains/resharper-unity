using UnityEngine;
using UnityEngine.Serialization;

public class Test01
{
    [FormerlySerializedAs("myValue")] private int myValue2;
    [FormerlySerializedAs("myConstant")] private const int myValue3 = 42;
    [FormerlySerializedAs("myStatic")] private static int myValue4 = 42;
}

public class Test02 : MonoBehaviour
{
    [FormerlySerializedAs("myValue"), FormerlySerializedAs("foo")] public int myValue;

    // Both attributes are NOT redundant. They apply to the backing field and do not match the generated field's name
    [field: FormerlySerializedAs("foo"), FormerlySerializedAs("Value2")]
    public string Value2 { get; set; }
}
