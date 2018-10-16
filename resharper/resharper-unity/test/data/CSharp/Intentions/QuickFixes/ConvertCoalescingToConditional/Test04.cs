using UnityEngine;

public class Foo : MonoBehaviour
{
    public GameObject Method(GameObject o)
    {
        return o ?? this.{caret}gameObject;
    }
}

