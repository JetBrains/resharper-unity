using UnityEngine;

public class UpdateBreakpointScript : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.F))
        {
            Debug.Log("F");
        }
    }
}
