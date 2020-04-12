using UnityEditor.Callbacks;

public class TestClass
{
    [PostProcessScene]
    private static void Valid()
    {
    }

    [PostProcessScene]
    private void MissingStatic()
    {
    }

    [PostProcessScene]
    private static bool WrongReturnType()
    {
        return true;
    }

    [PostProcessScene]
    private static void WrongParameters(int a, string b)
    {
    }

    [PostProcessScene]
    private static void WrongTypeParameters<T1, T2>()
    {
    }
}
