using UnityEngine;

namespace DefaultNamespace
{
    public class MultiLineCacheConflictTest
    {
        public void Test(Transform t)
        {
            t.position = t.position + t.position + t.posit{caret}ion;

            var x = new Vector3(10, 0, 0);
            x += new Vector3(0, 10, 10);

            t.localPosition = x;
            
            t.position = t.position + t.position;
        }
    }
}