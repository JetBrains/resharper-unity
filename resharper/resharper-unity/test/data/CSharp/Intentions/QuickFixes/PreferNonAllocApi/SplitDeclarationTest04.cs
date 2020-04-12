using UnityEngine;

namespace DefaultNamespace
{
    public class SplitDeclarationTest04
    {
        public void Test()
        {
            Collider[] a = null, b = null, size = Physics.Over{caret}lapBox(Vector3.zero, new Vector3(1, 1, 1), Quaternion.identity, 0, QueryTriggerInteraction.Collide), c = null, d = null; 
        }
    }
}