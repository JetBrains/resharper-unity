using UnityEngine;

namespace DefaultNamespace
{
    public class PropertyReuseTest
    {
        private static readonly int Property { get; } = Shader.PropertyToID("test")
            
        public void Method(Material material)
        {
            material.SetFloat("te{caret}st", 10.0f);
        }
    }
}