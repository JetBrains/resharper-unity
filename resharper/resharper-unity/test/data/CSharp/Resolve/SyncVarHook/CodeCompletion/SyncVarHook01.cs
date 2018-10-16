using UnityEngine;
using UnityEngine.Networking;

public class Foo : NetworkBehaviour
{
    [SyncVar(hook = "{caret}")]
    public int Value;

    public void OnValueChanged(int newValue)
    {
    }

    public static void AnotherValidMethod(int newValue)
    {
    }

    private static int SomeOtherValue(int newValue, int anotherValue)
    {
        return 42;
    }
}
