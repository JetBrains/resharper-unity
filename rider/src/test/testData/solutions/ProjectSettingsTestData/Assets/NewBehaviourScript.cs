using UnityEngine;
using UnityEngine.SceneManagement;

public class NewBehaviourScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        SceneManager.LoadScene("ImpossibleShortName");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
