using UnityEngine;

namespace DefaultNamespace
{
    public partial class TestClass
    {
        public void Method(Material material)
        {
            material.SetFloat("te{caret}st", 10.0f);
        }
    }

    public partial class TestClass
    {
        private static readonly int Test = Shader.PropertyToID("test");
    }
}