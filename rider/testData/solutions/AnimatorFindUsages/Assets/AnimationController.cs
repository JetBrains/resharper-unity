using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class AnimationController : StateMachineBehaviour
{
    public int X;
    public UnityEvent eventZ;

    public void Foo()
    {
        // should be more than 1 usage
        X = 0;
    }
}
