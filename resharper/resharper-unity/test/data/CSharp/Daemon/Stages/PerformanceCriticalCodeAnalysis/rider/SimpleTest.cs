using UnityEngine;

namespace DefaultNamespace
{
    public class SimpleTest : MonoBehaviour
    {
        private RigidBody2D myRigidBody2D;
        public void Update()
        {
            if (myRigidBody2D == null)
            {
                myRigidBody2D = GetComponent<RigidBody2D>();
            }
        }
    }
}