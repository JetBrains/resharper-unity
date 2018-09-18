//${RUN:1}
using UnityEngine;
using A;

namespace A
{
    public class TestComponent : MonoBehaviour
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
    public class Test08 : MonoBehaviour
    {
        public void Method()
        {
            GetComponent("{caret}TestComponent");
        }
    }
}