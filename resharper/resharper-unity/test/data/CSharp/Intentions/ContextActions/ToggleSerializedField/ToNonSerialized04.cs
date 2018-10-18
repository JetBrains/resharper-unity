using UnityEngine;
using JetBrains.Annotations;

public class Foo : MonoBehaviour
{
    [SerializeField] [NotNull] private int my{caret}Value;
}
