using UnityEditor;
using UnityEditor.Callbacks;

public class TestClass
{
    [PostProcessBuild]
    private static void Valid(BuildTarget target, string pathToBuildProject)
    {
    }

    [PostProcessBuild]
    private void MissingStatic(BuildTarget target, string pathToBuildProject)
    {
    }

    [PostProcessBuild]
    private static bool WrongReturnType(BuildTarget target, string pathToBuildProject)
    {
        return true;
    }

    [PostProcessBuild]
    private static void WrongParameters(int a, string b)
    {
    }

    [PostProcessBuild]
    private static void WrongTypeParameters<T1, T2>(BuildTarget target, string pathToBuildProject)
    {
    }
}
