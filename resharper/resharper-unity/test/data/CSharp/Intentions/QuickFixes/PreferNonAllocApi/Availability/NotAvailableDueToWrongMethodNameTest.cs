using UnityEngine;

public class NotAvailableDueToWrongMethodNameTest : MonoBehaviour
{
    public void Method()
    {
        Physics.Raycas(new Ray(Vector3.zero, Vector3.back));
    }
}
