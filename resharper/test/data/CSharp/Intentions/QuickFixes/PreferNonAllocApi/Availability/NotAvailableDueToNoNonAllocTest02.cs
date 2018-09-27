using UnityEngine;

public class NotAvailableDueToNoNonAllocTest : MonoBehaviour
{
    public void Method()
    {
        Physics.Raycast(new Ray(Vector3.zero, Vector3.back));
    }
}
