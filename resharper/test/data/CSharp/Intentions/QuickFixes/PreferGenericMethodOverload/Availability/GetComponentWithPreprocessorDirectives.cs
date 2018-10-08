using UnityEngine;

public class GetComponentWithPreprocessorDirectives
{
    public void Method(GameObject go)
    {
        go.GetComponent(
#if DEBUG
            "MyDebugComponent"
#else
            "MyReleaseComponent"
#endif
        );
    }
}

public class MyDebugComponent : MonoBehaviour
{
}

public class MyReleaseComponent : MonoBehaviour
{
}
