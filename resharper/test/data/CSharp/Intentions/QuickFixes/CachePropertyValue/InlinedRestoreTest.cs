using UnityEngine;

namespace DefaultNamespace
{
    public class InlinedRestoreTest
    {
        public void Test(Transform t)
        {
            t.position = t.position + t.pos{caret}ition;
            t.position = t.position + t.position + (t.position = Vector3.Back) + t.localPosition;
        }
    }
}