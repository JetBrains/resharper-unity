using UnityEngine;

public class NotAvailableDueToIncorrectSignatureTest : MonoBehaviour
{
    public void Method()
    {
        Physics.RaycastAll();   
    }
}
