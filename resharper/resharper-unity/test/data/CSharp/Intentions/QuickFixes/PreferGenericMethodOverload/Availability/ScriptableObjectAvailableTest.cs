using UnityEngine;

namespace DefaultNamespace
{
    public class Foo : ScriptableObject
    {
        
    }

    public class Test06
    {
        public void Test()
        {
            ScriptableObject.CreateInstan{caret}ce("Foo");
        }
    }
}