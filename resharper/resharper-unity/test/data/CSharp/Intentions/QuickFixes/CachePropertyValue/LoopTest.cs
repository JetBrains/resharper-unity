using UnityEngine;

namespace DefaultNamespace
{
    public class LoopTest
    {
        public void Test(Transform t)
        {
            t.position = t.position + t.position;
            while (true)
            {
                t.position = t.position + t.posi{caret}tion;
            }
        }
    }
}