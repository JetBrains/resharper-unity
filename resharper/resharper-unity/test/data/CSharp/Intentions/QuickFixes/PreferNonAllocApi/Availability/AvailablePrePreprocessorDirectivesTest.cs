using UnityEngine;

public class AvailablePrePreprocessorDirectivesTest : MonoBehaviour
{
    public void Method()
    {
        Physics.RaycastAll(new Ray(
            #if true
            Vector3.zero
            #else
            Vector3.back
            #endif
            , Vector3.zero));   
    }
}
