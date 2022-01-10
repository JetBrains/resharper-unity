using UnityEngine;

namespace DefaultNamespace
{
    public class IfTest
    {
        public void Test(Transform t)
        {
            if (true)
            {
                t.position = t.position + t.position;
            }
            else
            {
                t.position = t.position + t.posi{caret}tion;

            }
        }
    }
}