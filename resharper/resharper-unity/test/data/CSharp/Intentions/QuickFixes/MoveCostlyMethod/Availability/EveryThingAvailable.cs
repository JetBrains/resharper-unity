using UnityEngine;

namespace DefaultNamespace
{
    public class EveryThingAvailable : MonoBehaviour
    {
        public void Update()
        {
            for (int i = 0; i < 100; i++)
            {
                var transform = GetComponent<Transform>();
            }
        }
    }
}