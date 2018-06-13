using UnityEngine;

public class Foo : MonoBehaviour
{
  [HideInInspector] [SerializeField] private int myValue, my{caret:Remove:'HideInInspector':attribute:from:'myValue2'}Value2, myValue3;
}
