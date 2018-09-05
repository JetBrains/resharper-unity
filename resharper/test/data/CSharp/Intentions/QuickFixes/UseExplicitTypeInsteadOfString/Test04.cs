using UnityEngine;

public class Test04 
{
    class Whatever
    {
        
    }
    
    public void Method()
    {
        ScriptableObject.CreateInstance("Wh{caret}atever");
    }
}