using UnityEngine;

public class Available03 : MonoBehaviour
{
    public void Method()
    {
        Physics.BoxCastAll(Vector3.zero, Vector3.down,Vector3.forward);
    }
}
