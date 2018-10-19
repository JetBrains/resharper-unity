using UnityEngine;

namespace DefaultNamespace
{
    public class UnityObjectEqTest : MonoBehaviour
    {
        private GameObject gameObject = null;
        public void Update()
        {
            IndirectCostly();
        }

        private void IndirectCostly()
        {
            if (gameObject == null)
            {
                // smth..
            }
        }
    }
}