using UnityEngine;

namespace DefaultNamespace
{
    public class SplitDeclarationTest02
    {
        public void Test()
        {
            Collider[] b = null, size = Physics.Over{caret}lapBox(Vector3.zero, new Vector3(1, 1, 1), Quaternion.identity, 0, QueryTriggerInteraction.Collide), b = DoMagic(size); 
        }
    }
}