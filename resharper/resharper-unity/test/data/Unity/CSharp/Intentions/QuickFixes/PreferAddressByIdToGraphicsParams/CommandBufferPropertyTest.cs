using UnityEngine;
using UnityEngine.Rendering;

namespace DefaultNamespace
{
    public class CommandBufferPropertyTest
    {
        public void Test()
        {
            var cmdBuffer = new CommandBuffer();
            cmdBuffer.SetGlobalFloat("_off{caret}set", 1.5f);
        }
    }
}