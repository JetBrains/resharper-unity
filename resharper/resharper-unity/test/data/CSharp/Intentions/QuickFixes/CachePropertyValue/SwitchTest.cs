using UnityEngine;

namespace DefaultNamespace
{
    public class SwitchTest
    {
        public void Test(Transform t)
        {
            int x = 0;
            t.position = t.position + t.position;
            switch (x)
            {
                case 0:
                    t.position = t.position + t.posi{caret}tion;
                    break;
                case 1:
                    t.position = t.position + t.position;
                    break
            }
        }
    }
}