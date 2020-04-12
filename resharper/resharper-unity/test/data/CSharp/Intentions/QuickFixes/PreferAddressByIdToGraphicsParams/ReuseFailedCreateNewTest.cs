using UnityEngine;

namespace JetBrains.ReSharper.Psi.CSharp.Tree
{
    public class ReuseFailedCreateNewTest
    {
        private static readonly int Test = Shader.PropertyToID("test0");

        public void Method(Material material)
        {
            material.SetFloat("te{caret}st", 10.0f);
        }
    }
}