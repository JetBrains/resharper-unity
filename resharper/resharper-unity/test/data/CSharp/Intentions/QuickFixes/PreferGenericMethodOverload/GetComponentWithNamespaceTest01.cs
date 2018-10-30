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
    public class GetComponentWithNamespaceTest02
    {
        public void TestMethod(GameObject go)
        {
            go.GetComponent("A.B.C.Tes{caret}tMono");
        }
    }
}