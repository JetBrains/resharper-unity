using UnityEngine;
using UnityEngine.EventSystems;

public class even : MonoBehaviour
{
    public void evenTest12(BaseEventData eventData)
    {
        Debug.Log("Test12");
    }
    
    public void evenTest12Mod(BaseEventData eventData)
    {
        Debug.Log("evenTest12Mod");
    }

    public void evenTestModification(BaseEventData eventData)
    {
        Debug.Log("evenTestModification");
    }
    
    public static void evenTest12Static(BaseEventData eventData)
    {
        Debug.Log("evenTest12Static");
    }
}
