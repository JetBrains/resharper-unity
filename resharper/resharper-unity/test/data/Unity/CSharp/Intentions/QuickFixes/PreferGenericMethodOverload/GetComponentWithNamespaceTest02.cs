using UnityEngine;

namespace A
{
    namespace B
    {
        namespace C
        {
            public class TestMono : MonoBehaviour
            {
                
            }
        }
    }
}

namespace DefaultNamespace
{
    public class GetComponentWithNamespaceTest01
    {
        public void TestMethod(GameObject go)
        {
            go.GetComponent("Test{caret}Mono");
        }
    }
}