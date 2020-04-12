using UnityEngine;

public class NotAvailableDueToIncorrectSignatureTest02: MonoBehaviour
{
    public void Method()
    {
        Physics.BoxCastAll(new Ray(Vector3.zero, Vector3.back));
    }
}
