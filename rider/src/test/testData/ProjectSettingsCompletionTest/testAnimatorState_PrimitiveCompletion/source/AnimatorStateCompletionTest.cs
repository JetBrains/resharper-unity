using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NewBehaviourScript : MonoBehaviour
{
    private Animator _myAnimator;

    private void OnCollisionEnter2D(){
        _myAnimator.Play(<caret>, 0, 0);
    }
}
