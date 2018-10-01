using UnityEngine;

namespace DefaultNamespace
{
    public class IndirectCostlyTest : MonoBehaviour
    {
        private Object[] container = null;
        public void Update()
        {
            IndirectCostly();
        }

        private void IndirectCostly()
        {
            if (container == null)
            {
                container = Object.FindObjectsOfType<SimpleTest>();
            }
        }
    }
}