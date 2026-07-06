using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{

    private void Start()
    {
        Debug.Log("Start");
    }

    void Update()
    {
         // check that C# LangLevel is 7.3, fails if it is 7.1
         int binaryNotation = 0b_0001_1110_1000_0100_1000_0000; // 2 million
         Debug.Log(binaryNotation);

         // Load an additional scene additively (once) so scene handle extraction has at least 2 loaded scenes.
         // Appended after the statements above so the breakpoint line numbers used by other tests stay stable.
         if (!myAdditiveSceneLoaded)
         {
             myAdditiveSceneLoaded = true;
             UnityEngine.SceneManagement.SceneManager.LoadScene(
                 "AdditiveScene",
                 UnityEngine.SceneManagement.LoadSceneMode.Additive);
         }
    }

    private bool myAdditiveSceneLoaded;
}
