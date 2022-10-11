using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    public void OnJump1()
    {
        Debug.Log("MyOnJump1");
    }
    
    public void OnJump1WithPrefab()
    {
        Debug.Log("OnJump1WithPrefab");
    }

    public void OnJump()
    {
        Debug.Log("MyOnJump");
    }
}
