using UnityEngine;

namespace DefaultNamespace
{
    public class Foo<T> : MonoBehaviour
    {
        
    }

    public class Test06
    {
        public void Test(GameObject go)
        {
            go.AddComponent("Foo");
        }
    }
}