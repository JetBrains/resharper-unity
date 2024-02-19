using UnityEngine;

public class UpdateBreakpointScript : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.F))
        {
            // ReSharper disable once Unity.PerformanceCriticalCodeInvocation
            Debug.Log("F");
        }
    }
}
