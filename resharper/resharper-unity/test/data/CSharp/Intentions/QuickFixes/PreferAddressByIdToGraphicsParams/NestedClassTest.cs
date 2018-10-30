using UnityEngine;

namespace JetBrains.ReSharper.Psi.CSharp.Tree
{
    public class NestedClassTest
    {
        public class Nested
        {
            private string Test = null;  // for  unique name testing.
            
            public void Method(Material material)
            {
                material.SetFloat("te{caret}st", 10.0f);
            }
        }
    }
}