using UnityEditor.Callbacks;

public class TestClass
{
    [DidReloadScripts]
    private static void Valid()
    {
    }

    [DidReloadScripts]
    private void MissingStatic()
    {
    }

    [DidReloadScripts]
    private static bool WrongReturnType()
    {
        return true;
    }

    [DidReloadScripts]
    private static void WrongParameters(int a, string b)
    {
    }

    [DidReloadScripts]
    private static void WrongTypeParameters<T1, T2>()
    {
    }

    [DidReloadScripts]
    private int AllWrong(int value)
    {
        return 0;
    }
}
