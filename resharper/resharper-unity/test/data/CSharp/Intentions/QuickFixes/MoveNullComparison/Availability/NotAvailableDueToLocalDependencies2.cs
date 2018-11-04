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
            int i = 0;
            if (MyGetComponent(i) == null) {
                
            }
        }
    }
}