using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NewBehaviourScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        SceneManager.LoadScene("Disabled<selstart>Scene<selend>");
    }

    // Update is called once per frame
    void Update()
    {

    }
}
