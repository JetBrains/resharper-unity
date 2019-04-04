using UnityEngine;

namespace DefaultNamespace
{
    public class CommonTest : MonoBehaviour
    {
        private RigidBody2D myRigidBody2D;

        public void Start() 
        {

        }

        public void Test()
        {
        }
            
        public void Update()
        {
            if (myRigidBody2D == null)
            {
                myRigidBody2D = GetComponent<RigidBody2D>();
            }

            Test();
            IndirectCostly();
            OnTriggerEnter2D(null);

        }

        void OnTriggerEnter2D(Collider2D collision)
        {

        }
    }
}