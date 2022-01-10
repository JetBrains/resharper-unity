using UnityEngine;

public class A : MonoBehaviour
{
    private void Example()
    {
        InvokeRepeating("UndefinedMethod", 2, 0.3F);
    }
}
