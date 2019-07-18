using UnityEngine;

public class Foo : MonoBehaviour
{
  [SerializeField] private int myValue, my{caret:Add:'Header':before:'myValue2'}Value2, myValue3;
}
