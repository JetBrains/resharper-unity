// ReSharper disable Unity.RedundantEventFunction
using UnityEditor;
using UnityEngine;

public class HighlightExactMatchOnly : MonoBehaviour
{
    public void Start()
    {
    }

    public void Start(int i)
    {
    }
}

public class HighlightMultipleExactMatches : MonoBehaviour
{
    // Both overloads are correct - collisionInfo is optional.
    // Which is picked is ambiguous, so mark both with a warning
    public void OnCollisionStay(Collision collisionInfo)
    {
    }

    public void OnCollisionStay()
    {
    }
}

// Incorrect signatures should still be marked as event functions,
// as long as there isn't an exact match
public class HighlightIncorrectOverloads : MonoBehaviour
{
    public void Start(int i)
    {
    }

    public int Update()
    {
        return 0;
    }

    public void OnCollisionStay(Collision collisionInfo, int i)
    {
    }

    // Missing all parameters
    public void OnAnimatorIK()
    {
    }
}

public class StaticModifier : AssetPostprocessor
{
    // Should be static
    public void OnGeneratedCSProjectFiles()
    {
    }

    bool OnPreGeneratingCSProjectFiles()
    {
    }

    // Should not be static
    static void OnPreprocessAssembly(string pathName)
    {
    }
}

public class TypeParameters : MonoBehaviour
{
    public void Start<T1, T2>()
    {
    }
}

public class AllWrong : AssetPostprocessor
{
    public int OnGeneratedCSProjectFiles<T1, T2>(int value)
    {
        return 42;
    }
}
