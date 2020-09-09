using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    void Start() {
        Debug.Log("Start");
    }

    void Update()
    {
        Debug.Log("Update");
    }

    void OnApplicationQuit()
    {
        Debug.Log("Quit");
    }
}
