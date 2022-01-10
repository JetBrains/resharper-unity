using UnityEngine;

namespace DefaultNamespace
{
    public class LabmdaTest
    {
        public void Test(Transform t)
        {
            var lambda = () => t.position + t.position + t.posi{caret}tion;
        }
    }
}