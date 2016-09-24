using UnityEngine;

public class A : MonoBehaviour
{
    private void Example()
    {
        Invoke("UndefinedMethod", 2);
    }
}
