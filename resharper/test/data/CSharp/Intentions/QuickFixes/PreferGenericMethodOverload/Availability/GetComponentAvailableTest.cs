using UnityEngine;

public class Whatever : MonoBehaviour
{
        
}

public class Test01
{
    public void Method(GameObject o)
    {
        o.GetComponent("Whateve{caret}r");
    }
}
