using UnityEditor.Callbacks;

public class TestClass
{
    [OnOpenAsset]
    private static bool Valid(int instanceID, int line)
    {
        return true;
    }

    [OnOpenAsset]
    private bool MissingStatic(int instanceID, int line)
    {
        return true;
    }

    [OnOpenAsset]
    private static void WrongReturnType(int instanceID, int line)
    {
    }

    [OnOpenAsset]
    private static bool WrongParameters()
    {
        return true;
    }

    [OnOpenAsset]
    private static bool WrongTypeParameters<T1, T2>(int instanceID, int line)
    {
        return true;
    }
}
