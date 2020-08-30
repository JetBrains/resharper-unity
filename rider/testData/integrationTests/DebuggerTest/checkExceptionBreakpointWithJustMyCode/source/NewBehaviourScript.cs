using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start() => Debug.Log("Play");

    private void OnApplicationFocus(bool hasFocus) => Debug.Log(hasFocus ? "Play" : "Pause");

    private void OnApplicationQuit() => Debug.Log("Idle");

    // Update is called once per frame
    void Update()
    {
         throw new System.ArgumentException();
    }
}
