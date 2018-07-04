using UnityEngine;

public class Foo : MonoBehaviour
{
  [SerializeField] private int myValue, my{caret:Annotate:field:'myValue2':with:'HideInInspector':attribute}Value2, myValue3;
}
