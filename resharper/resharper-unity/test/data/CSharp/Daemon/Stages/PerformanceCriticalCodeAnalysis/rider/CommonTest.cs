using UnityEngine;

namespace DefaultNamespace
{
    public class CommonTest : MonoBehaviour
    {
        private RigidBody2D myRigidBody2D;

        public void Test()
        {
            Update();
        }
            
        public void Update()
        {
            if (myRigidBody2D == null)
            {
                myRigidBody2D = GetComponent<RigidBody2D>();
            }

            Test();
            IndirectCostly();

            Test2();
            Test2();
            Test2();
            Test2();
        }
        
        private void IndirectCostly()
        {
            var temp = gameObject.GetComponent<RigidBody2D>();
        }

        private void Test2()
        {
            IndirectCostly();
        }
    }
}