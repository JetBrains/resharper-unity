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

        private void FFF{caret}F()
        {

        }

        private void FFF2()
        {
            GetComponent<int>();
        }

        private void FFF3()
        {
            GetComponent<int>();
        }
    }
}