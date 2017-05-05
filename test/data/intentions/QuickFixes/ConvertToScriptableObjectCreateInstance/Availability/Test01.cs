using UnityEngine;

public class Foo : ScriptableObject
{
    public static Foo DoSomething()
    {
        var foo = new Foo();
        return foo;
    }
}
