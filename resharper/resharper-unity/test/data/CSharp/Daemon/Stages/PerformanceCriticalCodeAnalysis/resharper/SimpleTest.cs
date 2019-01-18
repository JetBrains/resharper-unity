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
            
            Debug.Log("");
            Debug.LogFormat("");
            Debug.LogError("");
            Debug.LogErrorFormat("");
            Debug.LogException("");
            Debug.LogWarning("");
            Debug.LogWarningFormat("");
            Debug.LogAssertion("");
            Debug.LogAssertionFormat("");
        }
    }
}