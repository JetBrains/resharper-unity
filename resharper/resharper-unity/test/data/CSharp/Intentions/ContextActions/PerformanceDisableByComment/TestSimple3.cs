using UnityEngine;

namespace ContextActionsTests
{
    public class ContextActionsTests : MonoBehaviour
    {
        private void Start()
        {
            
        }

        private void Update()
        {
            FFFF();
            FFF2();
        }

        private void FFFF()
        {

        }

        private void FFF2()
        {
            GetComponent<int>();
        }

        private void FF{caret}F3()
        {
            GetComponent<int>();
        }
    }
}