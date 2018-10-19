using UnityEngine;

namespace DefaultNamespace
{
    public class InlinedCacheTest
    {
        public void Test(Transform t)
        {
            var x = (t.localPosition = Vector3.back) + t.position + t.positi{caret}on;
        }
    }
}