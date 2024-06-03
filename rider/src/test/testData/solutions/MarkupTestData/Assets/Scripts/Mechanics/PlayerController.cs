using UnityEngine;

namespace Platformer.Mechanics
{
    public class PlayerController : MonoBehaviour
    {
        public void OnJump()
        {
            Debug.Log("OnJump");
        }

        public void OnMyTest()
        {
              Debug.Log("OnMyTest");
        }

        public void OnMyTestViaArray()
        {
              Debug.Log("OnMyTestViaArray");
        }
        
        public void UnusedPublicMethod()
        {
            Debug.Log("UnusedPublicMethod");
        }
    }
}