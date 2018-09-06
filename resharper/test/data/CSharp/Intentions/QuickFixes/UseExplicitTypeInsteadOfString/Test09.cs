using UnityEngine;
using A;

namespace A
{
    public class TestComponent
    {
        
    }
}

namespace B
{
    public class TestComponent : MonoBehaviour
    {
        
    }
}

namespace C
{
    public class Test09 : MonoBehaviour
    {
        public void Method()
        {
            GetComponent("{caret}TestComponent");
        }
    }
}
