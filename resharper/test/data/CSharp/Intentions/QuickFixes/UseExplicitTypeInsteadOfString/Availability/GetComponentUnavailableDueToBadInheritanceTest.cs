using UnityEngine;

public class Whatever : Component
{
        
}

public class Test03
{
    public void Method(GameObject o)
    {
        o.GetComponent("Whateve{caret}r");
    }
}
