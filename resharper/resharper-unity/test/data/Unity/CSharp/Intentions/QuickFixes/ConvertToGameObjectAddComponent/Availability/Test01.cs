using UnityEngine;

public class Foo : MonoBehaviour
{
    public static Foo DoSomething()
    {
        var foo = new Foo();
        return foo;
    }
}
