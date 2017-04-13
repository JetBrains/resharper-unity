using UnityEngine;

public class Test : MonoBehaviour
{
    public void Start()
    {
    }

    public void Update()
    {
        // With a comment
    }

    public void FixedUpdate()
    {
#if UNITY_EDITOR
#endif
    }

    public void LateUpdate()
    {
        int i;
    }
}
