using UnityEngine;

namespace JetBrains.ReSharper.Psi.CSharp.Tree
{
    public class UnderscoreNameTest
    {
        public void Method(Material material)
        {
            material.SetFloat("_te{caret}st", 10.0f);
        }
    }
}