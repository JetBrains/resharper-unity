using UnityEngine;

public class Available02 : MonoBehaviour
{
    public void Method()
    {
        Physics.OverlapBox(Vector3.zero, new Vector3(1, 1, 1), Quaternion.identity); 
    }
}
