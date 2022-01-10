using UnityEngine;

namespace JetBrains.ReSharper.Psi.CSharp.Tree
{
    public class AnimatorPropertyTest
    {
        public void Method(Animator animator)
        {
            animator.SetFloat("te{caret}st", 10f, 10f, 10f);
        }
    }
}