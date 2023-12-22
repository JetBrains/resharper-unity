using UnityEngine;
using UnityEngine.EventSystems;

public class NewBehaviourScript : MonoBehaviour
{
    public void TestUsages1(BaseEventData eventData)
    {
        Debug.Log("TestUsages1");
    }

    public static void Test12(BaseEventData eventData)
    {
        Debug.Log("Test12");
    }
}
