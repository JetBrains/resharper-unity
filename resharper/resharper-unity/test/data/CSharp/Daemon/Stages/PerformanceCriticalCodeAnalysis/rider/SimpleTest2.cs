using UnityEngine;

namespace DefaultNamespace
{
    public class SimpleTest : MonoBehaviour
    {
        private int[,] arr = new int[5,5];
        private RigidBody2D myRigidBody2D;

        public void Start() 
        {
            var a = Vector3.up * 5f * 4f;
            arr[0, 0] = 5;
        }


        public void FixedUpdate()
        {
            var a = Vector3.up * 5f * 4f;
            arr[0, 1] = 5;
            if (myRigidBody2D == null)
            {
                myRigidBody2D = GetComponent<RigidBody2D>();
            }

            NotExpensive();
        }

        public void NotExpensive() {
            var a = Vector3.up * 5f * 4f;
            arr[0, 1] = 5;
        }
    }
}