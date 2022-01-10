//${RUN:1}
using UnityEngine;

namespace Test
{
    public class TestClass : MonoBehaviour
    {
        public void Update()
        {
            Te{caret}st();
        }

        public void Test()
        {
            GetComponent("test");
        }
    }
}