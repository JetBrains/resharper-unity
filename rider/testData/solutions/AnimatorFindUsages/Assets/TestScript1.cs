using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TestScript1 : MonoBehaviour
{
    public virtual void Setup(int b)
    {
        // should be more than 1 usage
        Setup(b);
    }
}
