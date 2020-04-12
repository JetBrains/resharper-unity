using UnityEngine;

namespace DefaultNamespace
{
    public class ShaderPropertyTest
    {
        public void Test()
        {
            Shader.SetGlobalColor("Main{caret}Color", Color.black);
        }
    }
}