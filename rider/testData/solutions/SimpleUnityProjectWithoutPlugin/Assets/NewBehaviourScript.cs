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
         // check that C# LangLevel is 7.3, fails if it is 7.1
         int binaryNotation = 0b_0001_1110_1000_0100_1000_0000; // 2 million
         Debug.Log("Step");
    }
}
