using UnityEngine;
using JetBrains.Annotations;

public class Foo : MonoBehaviour
{
    [NotNull] private int my{caret}Value;

    private void Update()
    {
        if (myValue > 0)
        {
            // Do something...
        }
    }
}
