using UnityEngine;

namespace DefaultNamespace
{
    public class EveryThingAvailable : MonoBehaviour
    {
        public GameObject MyGetComponent(int i)
        {
            return GetComponent<Transform>();
        }
        
        public void Update()
        {
            for (int i = 0; i < 100; i++)
            {
                var transform = MyGetComponent(i);
            }
        }
    }
}