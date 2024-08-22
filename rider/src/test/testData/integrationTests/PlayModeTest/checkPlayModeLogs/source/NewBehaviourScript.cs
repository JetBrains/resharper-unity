using UnityEngine;
using System.Threading;

public class NewBehaviourScript : MonoBehaviour
{
    void Start() {
        Debug.Log("Start");
        new Thread(() => { Debug.Log("StartFromBackgroundThread"); }).Start();
    }

    void Update()
    {
        Debug.Log("Update");
        new Thread(() => { Debug.Log("UpdateFromBackgroundThread"); }).Start();
    }

    void OnApplicationQuit()
    {
        Debug.Log("Quit");
        new Thread(() => { Debug.Log("QuitFromBackgroundThread"); }).Start();
    }
}
