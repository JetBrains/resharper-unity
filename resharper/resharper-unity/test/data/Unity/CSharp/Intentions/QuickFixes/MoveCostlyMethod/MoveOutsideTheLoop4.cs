//${RUN:2}
using UnityEngine;

namespace Test
{
    public class TestClass : MonoBehaviour
    {
        public void Update()
        {
            for (int i = 0; i < 100; i++) 
            {
                void Test() 
                {
                    for (int j = 0; j < 100; j++) 
                    {
                        var transform = GetCo{caret}mponent<Transform>();
                    }
                }

                Test();
            }
        }
    }
}