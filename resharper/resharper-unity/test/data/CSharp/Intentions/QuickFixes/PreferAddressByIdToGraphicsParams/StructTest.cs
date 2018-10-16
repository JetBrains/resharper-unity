using UnityEngine;

namespace DefaultNamespace
{
    public struct StructTest
    {
        public void Method(Material material)
        {
            material.SetFloat("te{caret}s", 10.0f);
        }
    }
}