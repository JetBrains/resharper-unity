using UnityEngine;

namespace JetBrains.ReSharper.Psi.CSharp.Tree
{
    public class SimpleTest
    {
        public void Method(Material material)
        {
            material.SetFloat("te{caret}st", 10.0f);
        }
    }
}