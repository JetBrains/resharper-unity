using UnityEngine;
using JetBrains.Annotations;

public class Foo : MonoBehaviour
{
    [SerializeField, NotNull] private int myValue, my{caret:Make:field:'myValue2':non-serialized}Value2, myValue3;
}
