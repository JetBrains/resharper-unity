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
    
    void AnimEventDouble()
    {
        Debug.Log("AnimEventHolder.AnimEventDouble");
    }
    
    void AnimEventWithControllerMod()
    {
        Debug.Log("AnimEventHolder.AnimEventWithControllerMod");
    }
}

