using UnityEngine;

namespace DefaultNamespace
{
    public class ReturnTest
    {
        public Vector3 Test(Transform t)
        {
            t.position = t.position + t.posi{caret}tion;
            return t.position;
        }
    }
}