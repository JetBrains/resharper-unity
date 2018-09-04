using UnityEngine;

public class Foo
{
    class Whatever
    {
        
    }
    
    public void Method(GameObject o)
    {
        if (o.GetComponent("Whate{caret}ver")) { }
    }
}
