using UnityEngine;

public class A : MonoBehaviour
{
    // Fix exception here #317
    [Non{caret}[SerializeField] public int Value;
}
