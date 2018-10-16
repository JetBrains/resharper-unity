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
    public class GetComponentWithNamespaceUnavailableTest
    {
        public void TestMethod(GameObject go)
        {
            go.GetComponent("B.C.T{caret}estMono");
        }
    }
}