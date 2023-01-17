using UnityEngine;

public class AnimEventHolder : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("AnimEventHolder.Update");
    }

    void AnimEvent()
    {
        Debug.Log("AnimEventHolder.AnimEvent");
    }
    
    void AnimEvent2()
    {
        Debug.Log("AnimEventHolder.AnimEvent");
    }
}

