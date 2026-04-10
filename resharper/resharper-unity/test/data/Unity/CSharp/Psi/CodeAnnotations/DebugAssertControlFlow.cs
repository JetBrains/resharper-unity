using UnityEngine;

public class DebugAssertControlFlow : MonoBehaviour
{
    public void Method(object obj)
    {
        Debug.Assert(obj != null);
        if (obj == null) // Bug: Rider thinks this is always false due to condition:false=>halt
        {
            Debug.Log("obj is null");
        }
    }

    public void MethodWithStringMessage(object obj)
    {
        Debug.Assert(obj != null, "obj should not be null");
        if (obj == null)
        {
            Debug.Log("obj is null");
        }
    }
}