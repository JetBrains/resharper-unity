using UnityEngine;

namespace DefaultNamespace
{
    public class SplitDeclarationTest03
    {
        public void Test()
        {
            Collider[] b = null, size = Physics.Over{caret}lapBox(Vector3.zero, new Vector3(1, 1, 1), Quaternion.identity, 0, QueryTriggerInteraction.Collide); 
        }
    }
}