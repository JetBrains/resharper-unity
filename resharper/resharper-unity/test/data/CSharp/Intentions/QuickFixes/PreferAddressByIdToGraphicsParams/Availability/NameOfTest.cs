using UnityEngine;

namespace DefaultNamespace
{
    public class NameOfTest
    {
        public void test(Animator animator)
        {
            animator.SetBool(nameof(animator), true);
        }
    }
}