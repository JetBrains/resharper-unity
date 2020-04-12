using UnityEngine;

namespace JetBrains.ReSharper.Psi.CSharp.Tree
{
    public class NewNameTest
    {
        private static int Test = 0;
        
        public void Method(Material material)
        {
            material.SetFloat("te{caret}st", 10.0f);
        }
    }
}