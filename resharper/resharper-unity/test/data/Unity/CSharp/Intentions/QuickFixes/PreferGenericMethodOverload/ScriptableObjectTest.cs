using UnityEngine;

public class Whatever : ScriptableObject
{
        
}

public class Test04 
{
    public void Method()
    {
        ScriptableObject.CreateInstance("Wh{caret}atever");
    }
}