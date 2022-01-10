using UnityEngine;

namespace JetBrains.ReSharper.Psi.CSharp.Tree
{
    public class ReuseTest
    {
        private static readonly int Test2 = Shader.PropertyToID("test");

        public void Method(Material material)
        {
            material.SetFloat("te{caret}st", 10.0f);
        }
    }
}